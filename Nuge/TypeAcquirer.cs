using System;
using System.Collections.Generic;
using System.Linq;
using Acidmanic.Utilities.Reflection;

namespace nuge
{
    public class TypeAcquirer
    {
        
        private object TryInstantiate(Type type)
        {
            var constructor = type.GetConstructor(new Type[] { });

            if (constructor != null)
            {
                try
                {
                    var instance = constructor.Invoke(new object[] { });

                    if (instance != null)
                    {
                        return instance;
                    }
                }
                catch (Exception _)
                {
                    // ignored
                }
            }

            return null;
        }
        
        public List<T> AcquireAny<T>(List<Type> availableTypes)
        {
            var result = new List<T>();
            
            foreach (var type in availableTypes)
            {
                if (TypeCheck.InheritsFrom<T>(type))
                {
                    var instance = TryInstantiate(type);

                    if (instance != null)
                    {
                        result.Add((T) instance);
                    }
                }
            }
            return result;
        }

        public IEnumerable<Type> EnumerateModels(List<Type> availableTypes, string nameSpace)
        {
            return availableTypes.Where(t => TypeCheck.IsModel(t) && NamespaceMatch(nameSpace,t.Namespace));
        }

        public bool NamespaceMatch(string rootNs, string ns)
        {
            if (rootNs == null && ns == null)
            {
                return true;
            }

            if (rootNs == null)
            {
                return true;
            }

            if (ns == null)
            {
                return false;
            }

            if (rootNs.Length == 0)
            {
                return true;
            }

            if (ns.Length == 0)
            {
                return false;
            }

            return ns.StartsWith(rootNs);
        }
    }
}