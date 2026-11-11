using System;
using System.IO;
using System.Reflection;

namespace Spark.Tag4D.Core.Utils
{
    public static class AssemblyUtils
    {
        /// <summary>Folder that contains the assembly defining <paramref name="type"/>.</summary>
        public static string GetFolder(Type type) => Path.GetDirectoryName(GetFilePath(type)) ?? string.Empty;

        public static string GetFilePath(Type type)
        {
            // CodeBase handles shadow-copied/loaded-from-bytes hosts better than Location on .NET Framework,
            // but Location is the only API on .NET Core. Both point at the real file for add-in hosts.
            return Assembly.GetAssembly(type)!.Location;
        }
    }
}
