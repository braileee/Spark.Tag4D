using System;
using System.IO;
using System.Reflection;

namespace Spark.Tag4D.Core.Utils
{
    public static class AssemblyUtils
    {
        public static string GetFolder(Type type)
        {
            return Path.GetDirectoryName(GetFilePath(type));
        }

        public static string GetFilePath(Type type)
        {
            return Assembly.GetAssembly(type).Location;
        }
    }
}
