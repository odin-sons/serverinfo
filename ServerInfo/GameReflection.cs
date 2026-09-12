using System;
using System.Reflection;

namespace ServerInfo
{
    // Reaches Valheim's own types by name at runtime instead of a direct
    // reference, so the project builds without a local game install (see
    // README) and without a "publicized" assembly — reflection bypasses
    // accessibility, so private fields are reachable too
    internal static class GameReflection
    {
        private const BindingFlags AnyInstanceMember =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags AnyStaticMember =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

        public static Type FindType(string name)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type;
                try
                {
                    type = assembly.GetType(name, false);
                }
                catch
                {
                    continue;
                }

                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        public static object GetStaticMember(Type type, string name)
        {
            var prop = type.GetProperty(name, AnyStaticMember);
            if (prop != null)
            {
                return prop.GetValue(null);
            }

            var field = type.GetField(name, AnyStaticMember);
            return field?.GetValue(null);
        }

        public static object GetMember(object target, string name)
        {
            if (target == null)
            {
                return null;
            }

            var type = target.GetType();
            var field = type.GetField(name, AnyInstanceMember);
            if (field != null)
            {
                return field.GetValue(target);
            }

            var prop = type.GetProperty(name, AnyInstanceMember);
            return prop?.GetValue(target);
        }

        public static object Invoke(object target, string methodName, params object[] args)
        {
            if (target == null)
            {
                return null;
            }

            var method = target.GetType().GetMethod(methodName, AnyInstanceMember);
            return method?.Invoke(target, args);
        }
    }
}
