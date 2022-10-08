using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace NugZi
{
    class Program
    {
        static async  Task Main(string[] args)
        {

            await new PackageTree().Get("Acidmanic.MSLogging.LightWeight","1.0.1");
        }
    }
}
