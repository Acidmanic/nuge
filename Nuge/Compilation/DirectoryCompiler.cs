using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Acidmanic.Utilities.Results;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyInjection;
using nuge.Compilation.ProjectReferences;
using nuge.DotnetProject;
using nuge.Extensions;

namespace nuge.Compilation
{
    public class DirectoryCompiler
    {
        private readonly Nuget.Nuget _nuget;
        private readonly DirectoryInfo _tempDir;

        public DirectoryCompiler()
        {
            var executionPath = new DirectoryInfo(Assembly.GetExecutingAssembly().Location).Parent?.FullName;

            var nugetCachePath = Path.Join(executionPath, "nugetCache");

            var tempPath = Path.Join(executionPath, "temp");

            _tempDir = new DirectoryInfo(tempPath);

            if (_tempDir.Exists)
            {
                _tempDir.Delete(true);
            }

            _tempDir.Create();

            _nuget = new Nuget.Nuget(nugetCachePath);
        }

        public DirectoryCompiler WithLocalNuGetDirectory(params string[] directories)
        {
            if (directories != null)
            {
                foreach (var directory in directories)
                {
                    _nuget.AddLocalDirectoryPackageSource(directory);
                }
            }

            return this;
        }


        public CompiledDirectoryResult Compile(string directory)
        {
            var result = new CompiledDirectoryResult();

            result.ProjectsOnPath = DotnetProjectInfo.FindProjects(directory);

            var mergedProject = new MergedProject(result.ProjectsOnPath);

            result.AllIncludedProjects = mergedProject.InvolvedProjects;

            result.AllCSharpFiles = mergedProject.SourceFiles;

            result.Assembly = CompileTogether(mergedProject.SourceFiles, mergedProject.PackageReferences);

            return result;
        }


        public List<T> FastSearchFor<T>(string directory)
        {
            var projects = DotnetProjectInfo.FindProjects(directory);

            var sources = new List<FileInfo>();

            projects.ForEach(p => p.GetSourceCodes().ForEach(s => sources.Add(s)));

            var instances = new List<T>();

            foreach (var source in sources)
            {
                var compiled = TryInstantiate<T>(source);

                if (compiled)
                {
                    instances.AddRange(compiled.Value);
                }
            }

            return instances;
        }

        private Result<List<T>> TryInstantiate<T>(FileInfo source)
        {
            var contents = Read(new List<FileInfo> {source});

            var compiled = CompileCode(contents);

            try
            {
                var types = compiled.GetAvailableTypes();

                var instances = new TypeAcquirer().AcquireAny<T>(types);

                if (instances.Count > 0)
                {
                    //return Result<List<T>>.Successful(instances);
                    return new Result<List<T>>(true, instances);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return new Result<List<T>>().FailAndDefaultValue();
        }

        private List<FileInfo> GetNugetianRuntimes(List<PackageReference> nuGets)
        {
            var runtimes = new List<FileInfo>();

            if (nuGets != null)
            {
                foreach (var nuget in nuGets)
                {
                    var runtimeSet = _nuget
                        .GetCompilingReferences(nuget.PackageName, nuget.PackageVersion);

                    runtimes.AddRange(runtimeSet);
                }
            }

            return runtimes;
        }

        private List<FileInfo> ClearRepeatsSelectLatest(List<FileInfo> runtimes)
        {
            var cleared = new Dictionary<string, FileInfo>();

            var alreadyIncluded = new Dictionary<string, Version>();

            foreach (var runtimeFile in runtimes)
            {
                var runtimeAssemblyName = AssemblyName.GetAssemblyName(runtimeFile.FullName);

                string name = runtimeAssemblyName.Name;

                if (name != null)
                {
                    Version version = runtimeAssemblyName.Version;

                    if (alreadyIncluded.ContainsKey(name))
                    {
                        var alreadyVersion = alreadyIncluded[name];

                        if (alreadyVersion.CompareTo(version) < 0)
                        {
                            alreadyIncluded[name] = version;

                            cleared[name] = runtimeFile;
                        }
                    }
                    else
                    {
                        cleared.Add(name, runtimeFile);
                        alreadyIncluded.Add(name, version);
                    }
                }
            }

            return new List<FileInfo>(cleared.Values);
        }


        private Assembly CompileCode(IEnumerable<string> codes)
        {
            var parsedCodes = Parse(codes);

            var defaultRuntimes = CreateDefaultReferences();

            var references = new List<MetadataReference>();

            defaultRuntimes.ForEach(rt => references.Add(MetadataReference.CreateFromFile(rt.FullName)));

            var cSharpCompilation = CSharpCompilation.Create("ClassListingAssembly",
                parsedCodes,
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                    optimizationLevel: OptimizationLevel.Release,
                    assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default));

            using var peStream = new MemoryStream();

            var compilationResult = cSharpCompilation.Emit(peStream);

            if (!compilationResult.Success)
            {
                return null;
            }

            peStream.Seek(0, SeekOrigin.Begin);

            var assBytes = peStream.ToArray();

            var assembly = Assembly.Load(assBytes);

            return assembly;
        }

        private Assembly CompileTogether(List<FileInfo> files, List<PackageReference> nugets = null)
        {
            var codes = Read(files);

            var parsedCodes = Parse(codes);

            var runtimes = CreateDefaultReferences();

            runtimes.AddRange(GetNugetianRuntimes(nugets));

            runtimes = ClearRepeatsSelectLatest(runtimes);

            var references = new List<MetadataReference>();

            runtimes.ForEach(rt => references.Add(MetadataReference.CreateFromFile(rt.FullName)));

            var cSharpCompilation = CSharpCompilation.Create("ClassListingAssembly",
                parsedCodes,
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                    optimizationLevel: OptimizationLevel.Release,
                    assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default));

            using var peStream = new MemoryStream();

            var compilationResult = cSharpCompilation.Emit(peStream);

            if (!compilationResult.Success)
            {
                Console.WriteLine("Compilation done with error.");

                var failures = compilationResult.Diagnostics.Where(diagnostic =>
                    diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error);

                foreach (var diagnostic in failures)
                {
                    Console.Error.WriteLine("{0}: {1}", diagnostic.Id, diagnostic.GetMessage());
                }

                return null;
            }

            Console.WriteLine("Compilation done without any error.");

            peStream.Seek(0, SeekOrigin.Begin);

            var assBytes = peStream.ToArray();

            var writeAssembly = WriteAssembly(assBytes, "ClassListingAssembly");

            runtimes.ForEach(file => file.CopyTo(Path.Join(_tempDir.FullName, file.Name), true));

            var assembly = Assembly.LoadFrom(writeAssembly);

            return assembly;
        }

        private string WriteAssembly(byte[] bytes, string packageName)
        {
            var filePath = Path.Join(_tempDir.FullName, packageName + ".dll");

            File.WriteAllBytes(filePath, bytes);

            return filePath;
        }

        private List<string> Read(List<FileInfo> files)
        {
            var result = new List<string>();

            foreach (var file in files)
            {
                try
                {
                    var content = File.ReadAllText(file.FullName);

                    result.Add(content);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }

            return result;
        }

        private List<SyntaxTree> Parse(IEnumerable<string> codes)
        {
            var parsedCodes = new List<SyntaxTree>();

            var options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp10);

            foreach (var code in codes)
            {
                var parsedSyntaxTree = SyntaxFactory.ParseSyntaxTree(code, options);

                parsedCodes.Add(parsedSyntaxTree);
            }

            return parsedCodes;
        }

        private List<FileInfo> CreateDefaultReferences()
        {
            var assemblyFiles = new List<FileInfo>
            {
                new FileInfo(typeof(object).Assembly.Location),
                new FileInfo(typeof(IServiceCollection).Assembly.Location),
                GetRuntimeSpecificReference()
            };
            var file = new FileInfo(typeof(object).Assembly.Location).Directory;

            var others = file.EnumerateFiles("*.dll");

            foreach (var other in others)
            {
                try
                {
                    assemblyFiles.Add(other);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            
            Assembly.GetEntryAssembly()?.GetReferencedAssemblies().ToList()
                .ForEach(a => assemblyFiles.Add(new FileInfo(Assembly.Load(a).Location)));

            return assemblyFiles;
        }

        private static string GetAssemblyLocation<T>()
        {
            var typeOfT = typeof(T);

            return typeOfT.Assembly.Location;
        }

        private static PortableExecutableReference GetMetadataReference<T>()
        {
            var assemblyLocation = GetAssemblyLocation<T>();

            return MetadataReference.CreateFromFile(assemblyLocation);
        }

        private static FileInfo GetRuntimeSpecificReference()
        {
            var assemblyLocation = GetAssemblyLocation<object>();
            var runtimeDirectory = Path.GetDirectoryName(assemblyLocation);
            var libraryPath = Path.Join(runtimeDirectory, @"netstandard.dll");

            return new FileInfo(libraryPath);
        }
    }
}