namespace RJCP.MSBuildTasks
{
    using System;
    using Infrastructure;
    using Microsoft.Build.Framework;

    /// <summary>
    /// Be able to take a version string in SemVer2 format and break it up.
    /// </summary>
    public class SemVer : Microsoft.Build.Utilities.Task
    {
        /// <summary>
        /// Gets or sets the version in SemVer2 format that should be broken up.
        /// </summary>
        /// <value>The version to break up.</value>
        [Required]
        public string Version { get; set; }

        /// <summary>
        /// Get the version Major.Minor.Patch.Build version from the <see cref="Version"/> property.
        /// </summary>
        /// <value>The Major.Minor.Patch.Build version.</value>
        [Output]
        public string VersionBase { get; private set; }

        /// <summary>
        /// Get the version suffix after the <see cref="VersionBase"/> from the <see cref="Version"/> property.
        /// </summary>
        /// <value>the version suffix after the VersionBase.</value>
        [Output]
        public string VersionSuffix { get; private set; }

        /// <summary>
        /// Get the version informational suffix from <see cref="Version"/> property.
        /// </summary>
        /// <value>The informational version suffix.</value>
        [Output]
        public string VersionMeta { get; private set; }

        /// <summary>
        /// Executes the task to break down the <see cref="Version"/> property.
        /// </summary>
        /// <returns>Returns <see langword="true"/>, if successful</returns>
        public override bool Execute()
        {
            if (string.IsNullOrWhiteSpace(Version)) {
                Log.LogWarning(Resources.SemVer_VersionNotProvided);
                return false;
            }

            try {
                SemVer2 version = new SemVer2(Version);
                if (version.Major == 0 && version.Minor == 0 && version.Patch == 0) {
                    Log.LogWarning(Resources.SemVer_VersionNotSupported, Version);
                    return false;
                }

                // MSDN says that assemblies can have a max value of UInt16.MaxValue
                // - See https://docs.microsoft.com/en-us/dotnet/api/system.reflection.assemblyversionattribute
                if (version.Major >= ushort.MaxValue || version.Minor >= ushort.MaxValue ||
                    version.Patch >= ushort.MaxValue || version.Build >= ushort.MaxValue) {
                    Log.LogWarning(Resources.SemVer_VersionNotSupported, Version);
                    return false;
                }

                if (version.Build != 0) {
                    VersionBase = string.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Patch, version.Build);
                } else {
                    VersionBase = string.Format("{0}.{1}.{2}", version.Major, version.Minor, version.Patch);
                }

                VersionSuffix = version.PreRelease;
                VersionMeta = version.MetaData;
                return true;
            } catch (ArgumentException ex) {
                Log.LogWarning(ex.Message);
                return false;
            }
        }
    }
}
