namespace RJCP.MSBuildTasks.Infrastructure.Config
{
    using System;
    using System.IO;

    internal static class BuildFile
    {
        private static readonly string ConfigFile = ".rjbuild";

        public static IniFile Find(string path)
        {
            if (path is null) throw new ArgumentNullException(nameof(path));

            string fullPath = Path.GetFullPath(path);
            while(fullPath != null) {
                if (Directory.Exists(fullPath)) {
                    string configFile = Path.Combine(fullPath, ConfigFile);
                    if (File.Exists(configFile)) {
                        return new IniFile(configFile);
                    }
                }
                fullPath = Path.GetDirectoryName(fullPath);
            }
            return null;
        }
    }
}
