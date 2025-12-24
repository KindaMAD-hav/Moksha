using System;
using System.Collections.Generic;
using System.Linq;

namespace TinyGiantStudio.DevTools
{
    public static class DerivedClassFinder
    {
        public static List<Type> GetDerivedClasses<T>() where T : class
        {
            // Get the abstract class type
            Type baseType = typeof(T);

            // Get all types in the current AppDomain
            var allTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes());

            // Filter types that are derived from the base type and are not abstract
            var derivedTypes = allTypes
                .Where(type => type.IsSubclassOf(baseType) && !type.IsAbstract)
                .ToList();

            return derivedTypes;
        }
    }
}