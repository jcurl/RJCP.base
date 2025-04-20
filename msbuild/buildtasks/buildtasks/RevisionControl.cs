namespace RJCP.MSBuildTasks
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using Infrastructure.Process;
    using Infrastructure.SourceProvider;
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
        /// Executes the task to gather revision control information.
        /// </summary>
        /// <returns>Returns <see langword="true"/>, if successful</returns>
        public override bool Execute()
        {
            if (!CheckInputs()) return false;

            try {
                return ExecuteAsync().GetAwaiter().GetResult();
            } catch (RunProcessException ex) {
                StringBuilder sb = new StringBuilder();
                sb.Append(ex.Message)
                    .Append("\n ExitCode=").Append(ex.ExitCode)
                    .Append("\n Command=").Append(ex.Command)
                    .Append("\n WorkDir=").Append(ex.WorkingDirectory);
                Log.LogError(sb.ToString());
                sb.Clear().Append("Console:");
                if (ex.StdOut.Count > 0) {
                    foreach (string line in ex.StdOut) {
                        sb.Append("\n StdOut=").Append(line);
                    }
                }
                if (ex.StdErr.Count > 0) {
                    foreach (string line in ex.StdErr) {
                        sb.Append("\n StdErr=").Append(line);
                    }
                }
                Log.LogMessage(sb.ToString());
                return false;
            } catch (Exception ex) {
                Log.LogError(ex.Message);
                return false;
            }
        }

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

        private async Task<bool> ExecuteAsync()
        {
            ISourceControl provider = null;
            try {
                provider = await SourceFactory.Instance.CreateAsync(SourceType, Path);
            } catch (UnknownSourceProviderException ex) {
                Log.LogError(ex.Message);
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
            Task<SourceLabel> taskLabel = provider.IsTaggedAsync(Label, Path);
            await Task.WhenAll(taskBranch, taskCommit, taskShortCommit, taskTimeStamp, taskDirty, taskLabel);

            RevisionControlBranch = taskBranch.Result;
            RevisionControlCommit = taskCommit.Result;
            RevisionControlCommitShort = taskShortCommit.Result;
            RevisionControlDateTime = taskTimeStamp.Result.ToUniversalTime().ToString("yyyyMMdd\\THHmmss");
            RevisionControlDirty = taskDirty.Result.ToString();
            RevisionControlTagged = (taskLabel.Result == SourceLabel.LabelMatch).ToString();
            RevisionControlHost = Environment.MachineName;
            RevisionControlUser = Environment.UserName;

            switch (taskLabel.Result) {
            case SourceLabel.HeadNotFound:
                Log.LogWarning(Resources.RevisionControl_HeadNotFound);
                break;
            }

            if (StrictMode) {
                if (taskDirty.Result) {
                    Log.LogWarning(Resources.RevisionControl_IsDirty, Path);
                }

                switch (taskLabel.Result) {
                case SourceLabel.LabelMatch:
                    break;
                case SourceLabel.LabelMissing:
                    Log.LogWarning(Resources.RevisionControl_LabelMissing);
                    break;
                case SourceLabel.LabelNotFound:
                    Log.LogWarning(Resources.RevisionControl_LabelNotFound, Label);
                    break;
                case SourceLabel.LabelDiffers:
                    Log.LogWarning(Resources.RevisionControl_LabelDiffers, Label);
                    break;
                }
            }

            return true;
        }
    }
}
