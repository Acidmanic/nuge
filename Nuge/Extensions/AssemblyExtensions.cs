using System;
using System.Collections.Generic;
using System.Reflection;

namespace nuge.Extensions
{
    public static class AssemblyExtensions
    {
        public static List<Type> GetAvailableTypes(this Assembly assembly)
        {
            var result = new List<Type>();

            try
            {
                result.AddRange(assembly.GetTypes());
            }
            catch (Exception e)
            {
            }

            return result;
        }
    }
}