namespace RJCP.MSBuildTasks
{
    /// <summary>
    /// A task for clearing all cached data from the Revision Control task.
    /// </summary>
    /// <remarks>
    /// It is desirable to cache revision control information during a build. This task allows one to clear that cache,
    /// for example before starting a new build.
    /// </remarks>
    public class RevisionControlClearCache : Microsoft.Build.Utilities.Task
    {
        /// <summary>
        /// Gets or sets the path to the sources under revision control.
        /// </summary>
        /// <value>The path to the sources under revision control.</value>
        public string Path { get; set; }

        /// <summary>
        /// Executes the task to clear the cache.
        /// </summary>
        public override bool Execute()
        {
            if (string.IsNullOrEmpty(Path)) {
                Log.LogMessage("RevisionControlClearCache: Removing all cached revision control results");
                RevisionControl.ClearProviders();
                return true;
            }

            bool found = RevisionControl.ClearProviders(Path);
            if (found) {
                Log.LogMessage("RevisionControlClearCache: Removed cache for path {0}", Path);
            } else {
                Log.LogMessage("RevisionControlClearCache: No cache found for path {0}", Path);
            }
            return true;
        }
    }
}
