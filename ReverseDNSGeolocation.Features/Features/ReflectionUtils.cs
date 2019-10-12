namespace ReverseDNSGeolocation.Features
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Reflection;

    public static class ReflectionUtils
    {
        public static List<T> GetInstancesOfType<T>(params object[] constructorArgs) where T : class
        {
            List<T> instances = new List<T>();

            foreach (Type type in Assembly.GetAssembly(typeof(T)).GetTypes().Where(currentType => currentType.IsClass && !currentType.IsAbstract && currentType.IsSubclassOf(typeof(T))))
            {
                instances.Add((T)Activator.CreateInstance(type, constructorArgs));
            }

            return instances;
        }

        public static Dictionary<string, T> GetInstanceDictOfType<T>(params object[] constructorArgs) where T : class
        {
            var instances = GetInstancesOfType<T>(constructorArgs);

            var dict = new Dictionary<string, T>();

            foreach (var instance in instances)
            {
                dict[instance.GetType().Name] = instance;
            }

            return dict;
        }
    }
}
