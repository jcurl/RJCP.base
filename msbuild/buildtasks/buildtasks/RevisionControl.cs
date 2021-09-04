namespace RJCP.MSBuildTasks
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using Infrastructure.SourceProvider;
    using Infrastructure.Threading.Tasks;
    using Microsoft.Build.Framework;

    /// <summary>
    /// The task for checking Revision Control status.
    /// </summary>
    public class RevisionControl : Microsoft.Build.Utilities.Task
    {
        /// <summary>
        /// Gets or sets the type of revision control in use.
        /// </summary>
        /// <value>The type of revision control in use.</value>
        [Required]
        public string Type { get; set; }

        private string SourceType { get; set; }

        /// <summary>
        /// Gets or sets the path to the sources under revision control.
        /// </summary>
        /// <value>The path to the sources under revision control.</value>
        [Required]
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the label that is expected to be applied to test for an 'official' build.
        /// </summary>
        /// <value>The label that is expected to be applied to test for an 'official' build.</value>
        public string Label { get; set; }

        /// <summary>
        /// Gets or sets if strict mode is enabled or not.
        /// </summary>
        /// <value>If strict mode is enabled or not.</value>
        public string Strict { get; set; }

        private bool StrictMode { get; set; }

        /// <summary>
        /// Gets or sets if caching should be used. The default is to enable caching.
        /// </summary>
        /// <value>If caching should be used.</value>
        public string Cached { get; set; }

        private bool CachedMode { get; set; }

        /// <summary>
        /// Gets the type of the revision control that was detected.
        /// </summary>
        /// <value>The type of the revision control that was detected.</value>
        [Output]
        public string RevisionControlType { get; private set; }

        /// <summary>
        /// Gets the revision control branch if available.
        /// </summary>
        /// <value>The revision control branch if available.</value>
        [Output]
        public string RevisionControlBranch { get; private set; }

        /// <summary>
        /// Gets the last revision control commit for the path given.
        /// </summary>
        /// <value>The last revision control commit for the path given.</value>
        [Output]
        public string RevisionControlCommit { get; private set; }

        /// <summary>
        /// Gets the last short revision control commit for the path given.
        /// </summary>
        /// <value>The last short revision control commit for the path given.</value>
        [Output]
        public string RevisionControlCommitShort { get; private set; }

        /// <summary>
        /// Gets the last revision control date time commit for the path given.
        /// </summary>
        /// <value>The last revision control date time commit for the path given.</value>
        [Output]
        public string RevisionControlDateTime { get; private set; }

        /// <summary>
        /// Gets if the revision control shows files are modified.
        /// </summary>
        /// <value>A boolean if the revision control dirty at the path given.</value>
        [Output]
        public string RevisionControlDirty { get; private set; }

        /// <summary>
        /// Gets if the revision control tagged and not modified from <see cref="Label"/>.
        /// </summary>
        /// <value>A boolean if the revision control is tagged with <see cref="Label"/> at the path given.</value>
        [Output]
        public string RevisionControlTagged { get; private set; }

        /// <summary>
        /// Gets the name of the machine that this task is running on.
        /// </summary>
        /// <value>The name of the machine that this task is running on.</value>
        [Output]
        public string RevisionControlHost { get; private set; }

        /// <summary>
        /// Gets the name of the logged in user that this task is running on.
        /// </summary>
        /// <value>The name of the logged in user that this task is running on.</value>
        [Output]
        public string RevisionControlUser { get; private set; }

        /// <summary>
        /// Clears the providers cache completely.
        /// </summary>
        public static void ClearProviders()
        {
            Providers = new AsyncCache<(string, string), ISourceControl>();
        }

        /// <summary>
        /// Clears a specific source provider from the cache.
        /// </summary>
        /// <param name="path">The path that should be removed.</param>
        /// <returns>
        /// Returns <see langword="true"/> if at least one cache entry was found with a matching
        /// <paramref name="path"/>, <see langword="false"/> otherwise.
        /// </returns>
        public static bool ClearProviders(string path)
        {
            List<(string, string)> keys = new List<(string, string)>();
            bool found = Providers.Enumerate((e) => {
                if (e.Item2.Equals(path)) {
                    keys.Add(e);
                    return true;
                }
                return false;
            });

            if (found) {
                foreach (var key in keys) {
                    Providers.Remove(key);
                }
            }
            return found;
        }

        /// <summary>
        /// Executes the task to gather revision control information.
        /// </summary>
        /// <returns>Returns <see langword="true"/>, if successful</returns>
        public override bool Execute()
        {
            if (!CheckInputs()) return false;

            try {
                return ExecuteAsync().GetAwaiter().GetResult();
            } catch (InvalidOperationException ex) {
                Log.LogError(ex.Message);
                return false;
            }
        }

        // This cache contains the source providers while MSBuild is running and loaded this assembly. The assumption is
        // that the revision control system doesn't change for this path. MSBuild will instantiate a new task every time
        // it is run, but this cache is constant between all isntances. This can help speed up builds where the task is
        // run multiple times for the same project, e.g. with multiple target frameworks.
        private static AsyncCache<(string, string), ISourceControl> Providers =
            new AsyncCache<(string, string), ISourceControl>();

        private bool CheckInputs()
        {
            if (string.IsNullOrWhiteSpace(Path)) {
                Log.LogError(Resources.RevisionControl_PathNotDefined);
                return false;
            }

            if (File.Exists(Path)) {
                Log.LogError(Resources.RevisionControl_PathMustBeDirectory);
                return false;
            }

            if (!Directory.Exists(Path)) {
                Log.LogError(Resources.RevisionControl_PathDirectoryDoesntExist);
                return false;
            }

            SourceType = Type?.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(Type)) {
                Log.LogError(Resources.RevisionControl_RevisionControlTypeNotDefined);
                return false;
            }

            try {
                StrictMode = CheckBoolean(Strict, false);
            } catch (ArgumentException) {
                Log.LogError(Resources.RevisionControl_UnknownStrictMode);
                return false;
            }

            try {
                CachedMode = CheckBoolean(Cached, true);
            } catch (ArgumentException) {
                Log.LogError(Resources.RevisionControl_UnknownCachedMode);
                return false;
            }

            return true;
        }

        private static bool CheckBoolean(string value, bool defaultValue)
        {
            if (string.IsNullOrWhiteSpace(value)) {
                return defaultValue;
            } else {
                switch (value.Trim().ToLowerInvariant()) {
                case "yes":
                case "true":
                case "enabled":
                    return true;
                case "no":
                case "false":
                case "disabled":
                    return false;
                default:
                    throw new ArgumentException(Resources.Arg_UnknownValue, nameof(value));
                }
            }
        }

        private async Task<ISourceControl> GetProviderAsync(string sourceType, string path)
        {
            try {
                ISourceControl foundProvider =
                    await SourceFactory.Instance.CreateAsync(sourceType, path);
                if (foundProvider == null) return null;
                return foundProvider;
            } catch (UnknownSourceProviderException ex) {
                Log.LogError(ex.Message);
                return null;
            }
        }

        private async Task<bool> ExecuteAsync()
        {
            ISourceControl provider;

            if (CachedMode) {
                provider = await Providers.GetSetAsync((SourceType, Path), () => {
                    return GetProviderAsync(SourceType, Path);
                });
            } else {
                provider = await GetProviderAsync(SourceType, Path);
            }

            if (provider == null) {
                Log.LogError(Resources.RevisionControl_CantInstantiateProvider);
                return false;
            }

            RevisionControlType = provider.RevisionControlType;
            Task<string> taskBranch = provider.GetCurrentBranchAsync(Path);
            Task<string> taskCommit = provider.GetCommitAsync(Path);
            Task<string> taskShortCommit = provider.GetCommitShortAsync(Path);
            Task<DateTime> taskTimeStamp = provider.GetCommitDateTimeAsync(Path);
            Task<bool> taskDirty = provider.IsDirtyAsync(Path);
            Task<bool> taskLabel = string.IsNullOrWhiteSpace(Label) ?
                Task.FromResult(false) :
                provider.IsTaggedAsync(Label, Path);
            await Task.WhenAll(taskBranch, taskCommit, taskShortCommit, taskTimeStamp, taskDirty, taskLabel);

            RevisionControlBranch = taskBranch.Result;
            RevisionControlCommit = taskCommit.Result;
            RevisionControlCommitShort = taskShortCommit.Result;
            RevisionControlDateTime = taskTimeStamp.Result.ToUniversalTime().ToString("yyyyMMdd\\THHmmss");
            RevisionControlDirty = taskDirty.Result.ToString();
            RevisionControlTagged = taskLabel.Result.ToString();
            RevisionControlHost = Environment.MachineName;
            RevisionControlUser = Environment.UserName;

            if (StrictMode) {
                if (taskDirty.Result) {
                    Log.LogWarning(Resources.RevisionControl_IsDirty, Path);
                }

                if (!taskLabel.Result) {
                    if (!string.IsNullOrWhiteSpace(Label)) {
                        Log.LogWarning(Resources.RevisionControl_NotLabelled, Label, Path);
                    } else {
                        Log.LogWarning(Resources.RevisionControl_LabelNotDefined);
                    }
                }
            }

            return true;
        }
    }
}
