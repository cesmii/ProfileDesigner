namespace CESMII.ProfileDesigner.Common.Utils
{
    using System;
    using System.IO;
    using System.Reflection;

    /// <summary>
    /// Information about the executing assembly.
    /// </summary>
    public static class AssemblyInfoUtil
    {
        private static DateTime? date;

        private static string version;

        /// <summary>
        /// Gets the linker date from the assembly header.
        /// </summary>
        public static DateTime Date
        {
            get
            {
                if (date == null)
                {
                    date = GetLinkerTime(Assembly.GetExecutingAssembly());
                }

                return date.Value;
            }
        }

        public static string Version
        {
            get
            {
                if (string.IsNullOrEmpty(version))
                {
                    version = Assembly.GetEntryAssembly()?.GetName().Version.ToString();
                }

                return version;
            }
        }

        public static string AssemblyDirectory (this Assembly assembly)
        {
            string codeBase = assembly.Location;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            return Path.GetDirectoryName(path);
        }

        /// <summary>
        /// Gets the linker date of the assembly.
        /// </summary>
        /// <param name="assembly">The project assembly.</param>
        /// <returns>The assembly of current project.</returns>
        private static DateTime GetLinkerTime(this Assembly assembly)
        {
            // https://blog.codinghorror.com/determining-build-date-the-hard-way/
            // https://www.meziantou.net/getting-the-date-of-build-of-a-dotnet-assembly-at-runtime.htm
            var filePath = assembly.Location;

            // Get last write time on DLL (of current assembly, may be imperfect but close enough.)
            var createdOn = File.GetLastWriteTimeUtc(filePath);

            // Convert to local.
            var localTime = TimeZoneInfo.ConvertTimeFromUtc(createdOn, TimeZoneInfo.Local);

            return localTime;
        }
    }
}
