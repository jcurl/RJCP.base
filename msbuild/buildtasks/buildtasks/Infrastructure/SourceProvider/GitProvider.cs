namespace RJCP.MSBuildTasks.Infrastructure.SourceProvider
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Threading.Tasks;
    using Infrastructure.Config;
    using Infrastructure.Process;
    using Infrastructure.Threading.Tasks;
    using Infrastructure.Tools;

    /// <summary>
    /// The GIT Source Provider
    /// </summary>
    internal class GitProvider : ISourceControl
    {
        private readonly Executable m_Git;
        private readonly string m_Path;
        private string m_TopLevelPath;
        private IniFile m_Config;

        /// <summary>
        /// Gets the type of the revision control.
        /// </summary>
        /// <value>The type of the revision control, which is always "git" for this module.</value>
        public string RevisionControlType { get { return "git"; } }

        private GitProvider(Executable gitTool, string path)
        {
            if (gitTool == null)
                throw new ArgumentNullException(nameof(gitTool));
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            m_Git = gitTool;
            m_Path = path;
        }

        /// <summary>
        /// The method that creates a new GIT provider.
        /// </summary>
        /// <param name="path">The path to the repository, or a subdirectory of the repository.</param>
        /// <returns>The <see cref="GitProvider"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> cannot be found.</exception>
        /// <exception cref="InvalidOperationException">
        /// GIT returned unexpected output (repository may be invalid, tooling doesn't exist, etc.)
        /// </exception>
        /// <remarks>
        /// This method creates the object, as opposed to a constructor, so that it can be done asynchronously.
        /// </remarks>
        internal static async Task<GitProvider> CreateAsync(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (!Path.IsPathRooted(path))
                path = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, path));

            if (!Directory.Exists(path))
                throw new ArgumentException("Source path not found");

            Executable git = await ToolFactory.Instance.GetToolAsync(ToolFactory.GitTool);
            GitProvider provider = new GitProvider(git, path);
            await provider.SetTopLevelPathAsync();
            return provider;
        }

        /// <summary>
        /// Gets the top level path of the GIT repository from the instantiated path.
        /// </summary>
        /// <returns>A Task representing the asynchronous operation.</returns>
        /// <exception cref="InvalidOperationException">GIT returned unexpected output.</exception>
        /// <remarks>
        /// Tries to determine the top level path of the GIT repository from the path given in the constructor at the
        /// time this object is created with <see cref="CreateAsync(string)"/>.
        /// </remarks>
        private async Task SetTopLevelPathAsync()
        {
            m_TopLevelPath = await GetTopLevelPathAsync(m_Path);
            m_Config = BuildFile.Find(m_TopLevelPath);
        }

        private async Task<string> GetTopLevelPathAsync(string path)
        {
            RunProcess git = await m_Git.RunFromAsync(path, "rev-parse", "--show-toplevel");
            if (git.ExitCode != 0)
                throw new RunProcessException(Resources.Git_Error, git);
            if (git.StdOut.Count != 1)
                throw new RunProcessException(Resources.Git_UnexpectedOutput, git);
            if (string.IsNullOrWhiteSpace(git.StdOut[0]))
                throw new RunProcessException(Resources.Git_UnexpectedOutput, git);

            return Path.GetFullPath(git.StdOut[0]);
        }

        private readonly AsyncValue<string> m_Branch = new AsyncValue<string>();

        /// <summary>
        /// Gets the current branch for the repository.
        /// </summary>
        /// <param name="path">
        /// The path. The GIT provider doesn't use the path, as the branch is the same for all paths, so this parameter
        /// is ignored for the GIT provider.
        /// </param>
        /// <returns>
        /// The name of the branch. This may be <see cref="string.Empty"/> if the current branch is not known, or the
        /// revision control system doesn't support this.
        /// </returns>
        public Task<string> GetCurrentBranchAsync(string path)
        {
            return m_Branch.GetSetAsync(() => {
                return GetBranchInternalAsync();
            });
        }

        private async Task<string> GetBranchInternalAsync()
        {
            RunProcess git = await m_Git.RunFromAsync(m_TopLevelPath, "symbolic-ref", "-q", "--short", "HEAD");
            if (git.ExitCode == 1) return string.Empty;        // Not on a branch
            if (git.ExitCode != 0)
                throw new RunProcessException(Resources.Git_Error, git);

            return git.StdOut[0];                              // The name of the branch
        }

        private readonly AsyncCache<string, string> m_Commits = new AsyncCache<string, string>();

        /// <summary>
        /// Gets the last commit for the path given.
        /// </summary>
        /// <param name="path">The path to get the commit for.</param>
        /// <returns>The representation for the commit.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        public Task<string> GetCommitAsync(string path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            return GetCommitInternalAsync(path);
        }

        private Task<string> GetCommitInternalAsync(string path)
        {
            return m_Commits.GetSetAsync(path, async () => {
                RunProcess git = await m_Git.RunFromAsync(m_TopLevelPath, "log", "-1", "--format=%H", "--", path);
                if (git.ExitCode != 0)
                    throw new RunProcessException(Resources.Git_Error, git);
                if (git.StdOut.Count == 0) {
                    string message = string.Format(Resources.Git_LogNoCommits, path);
                    throw new InvalidOperationException(message);
                }
                if (git.StdOut.Count != 1) {
                    string message = string.Format(Resources.Git_LogUnexpectedOutput, path);
                    throw new RunProcessException(message, git);
                }

                return git.StdOut[0];
            });
        }

        /// <summary>
        /// Get commit short as an asynchronous operation.
        /// </summary>
        /// <param name="path">The path to get the commit for.</param>
        /// <returns>The short representation for the commit</returns>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        public async Task<string> GetCommitShortAsync(string path)
        {
            string commit = await GetCommitAsync(path);
            return commit.Substring(0, 7);
        }

        private readonly AsyncCache<string, DateTime> m_CommitDateTime = new AsyncCache<string, DateTime>();

        /// <summary>
        /// Gets the last commit date/time for the path given.
        /// </summary>
        /// <param name="path">The path to get the commit for.</param>
        /// <returns>
        /// The Date/Time in the timezone if possible, for the last commit. Note, that the timezone may have to be
        /// converted to UTC for proper comparison. If no timezone information is available, assume the current
        /// timezone.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Parsing the Date/Time resulted in an error.</exception>
        /// <exception cref="InvalidOperationException">The GIT tool returned unexpected output.</exception>
        /// <exception cref="FormatException">Parsing the Date/Time resulted in an error.</exception>
        public Task<DateTime> GetCommitDateTimeAsync(string path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            return GetCommitDateInternalAsync(path);
        }

        private Task<DateTime> GetCommitDateInternalAsync(string path)
        {
            return m_CommitDateTime.GetSetAsync(path, async () => {
                RunProcess git = await m_Git.RunFromAsync(m_TopLevelPath, "log", "-1", "--format=%cI", "--", path);
                if (git.ExitCode != 0)
                    throw new RunProcessException(Resources.Git_Error, git);
                if (git.StdOut.Count == 0) {
                    string message = string.Format(Resources.Git_LogNoCommits, path);
                    throw new RunProcessException(message, git);
                }
                if (git.StdOut.Count != 1) {
                    string message = string.Format(Resources.Git_LogUnexpectedOutput, path);
                    throw new RunProcessException(message, git);
                }
                try {
                    return ParseGitIso8601(git.StdOut[0]);
                } catch (Exception ex) {
                    throw new RunProcessException(Resources.Git_InvalidIso8601, git, ex);
                }
            });
        }

        /// <summary>
        /// Parses the returned Date/Time from GIT in ISO8601 format.
        /// </summary>
        /// <param name="datetime">The date and time in ISO8601 format.</param>
        /// <returns>The converted Date/Time</returns>
        /// <exception cref="ArgumentException">The <paramref name="datetime"/> length is incorrect.</exception>
        /// <exception cref="FormatException">Parsing the <paramref name="datetime"/> resulted in an error.</exception>
        /// <remarks>The ISO8601 format returned by GIT looks like: 2016-06-14T16:13:46+03:00</remarks>
        private static DateTime ParseGitIso8601(string datetime)
        {
            if (datetime.Length != 25) throw new ArgumentException(Resources.Git_InvalidIso8601, nameof(datetime));
            int year = int.Parse(datetime.Substring(0, 4), CultureInfo.InvariantCulture);
            int month = int.Parse(datetime.Substring(5, 2), CultureInfo.InvariantCulture);
            int day = int.Parse(datetime.Substring(8, 2), CultureInfo.InvariantCulture);
            int hour = int.Parse(datetime.Substring(11, 2), CultureInfo.InvariantCulture);
            int min = int.Parse(datetime.Substring(14, 2), CultureInfo.InvariantCulture);
            int sec = int.Parse(datetime.Substring(17, 2), CultureInfo.InvariantCulture);
            int tzh = int.Parse(datetime.Substring(19, 3), CultureInfo.InvariantCulture);
            int tzm = int.Parse(datetime.Substring(23, 2), CultureInfo.InvariantCulture);

            // All dates returned are local to UTC, as the ISO standard doesn't specify the timezone when it was
            // created, only the offset from UTC.
            return new DateTime(year, month, day, hour, min, sec, DateTimeKind.Utc) - new TimeSpan(tzh, tzm, 0);
        }

        private readonly AsyncCache<string, bool> m_Dirty = new AsyncCache<string, bool>();

        /// <summary>
        /// Determines whether the specified path is dirty.
        /// </summary>
        /// <param name="path">The path to check for.</param>
        /// <returns><see langword="true"/> if the specified path is dirty; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="InvalidOperationException">The GIT tool returned unexpected output.</exception>
        public async Task<bool> IsDirtyAsync(string path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            // First check that we have HEAD, if not, then a diff-index will fail
            string head = await GetHeadCommit();
            if (string.IsNullOrEmpty(head)) return true;

            return await IsDirtyInternalAsync(path);
        }

        private Task<bool> IsDirtyInternalAsync(string path)
        {
            // The algorithm only checks the dirty status for the precise path given. There is no logic to check if a
            // path is dirty if a child path has already been queried and is already considered dirty.
            return m_Dirty.GetSetAsync(path, async () => {
                RunProcess gitDiff = await m_Git.RunFromAsync(m_TopLevelPath, "diff-index", "--quiet", "HEAD", "--", path);
                if (gitDiff.ExitCode == 1) return true;
                if (gitDiff.ExitCode == 0) return false;
                throw new RunProcessException(Resources.Git_UnexpectedOutput, gitDiff);
            });
        }

        /// <summary>
        /// Checks if the repository is tagged.
        /// </summary>
        /// <param name="tag">The tag to check for.</param>
        /// <param name="path">The path to check that has the tag.</param>
        /// <returns>
        /// <see langword="true"/> if the specified tag exists, and the path given matches the contents of the tag;
        /// otherwise, <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// A tag in GIT applies to the entire repository. This method doesn't simply check that the current tag is the
        /// commit, it ensures that there are no differences between the current commit and the tag so that commits
        /// outside of the path don't result in false-negatives.
        /// </remarks>
        public async Task<SourceLabel> IsTaggedAsync(string tag, string path)
        {
            if (string.IsNullOrWhiteSpace(tag)) return SourceLabel.LabelMissing;

            Task<string> headCommit = GetHeadCommit();
            Task<string> tagCommit = GetTagInternalAsync(tag);
            string[] commits = await Task.WhenAll(headCommit, tagCommit);

            if (string.IsNullOrEmpty(commits[0]))
                return SourceLabel.HeadNotFound;
            if (string.IsNullOrEmpty(commits[1]))
                return SourceLabel.LabelNotFound;

            // Check if the user provided an override in the file ".rjbuild" in the section [git-overrides], that has
            // the key being the git hash which is the same as the current HEAD, and the values which are the possible
            // tags to override.
            if (m_Config is object) {
                if (m_Config.TryGetValue("git-overrides", out IniSection section)) {
                    if (section.TryGetValue(commits[0], out string tagList)) {
                        string[] tags = tagList.Split(',');
                        foreach (string foundTag in tags) {
                            if (string.Compare(tag, foundTag.Trim(), StringComparison.OrdinalIgnoreCase) == 0) {
                                return SourceLabel.LabelOverride;
                            }
                        }
                    }
                }
            }

            // Special case, if the commit of what is currently checked out matches the tag, they're a match.
            if (commits[0] == commits[1]) return SourceLabel.LabelMatch;

            bool diff = await GetDiffInternalAsync(path, commits[0], commits[1]);
            return diff ? SourceLabel.LabelDiffers : SourceLabel.LabelMatch;
        }

        private readonly AsyncValue<string> m_HeadCommit = new AsyncValue<string>();

        private Task<string> GetHeadCommit()
        {
            return m_HeadCommit.GetSetAsync(async () => {
                RunProcess git = await m_Git.RunFromAsync(m_TopLevelPath, "rev-parse", "HEAD");
                if (git.ExitCode == 128) return string.Empty;
                if (git.ExitCode == 0) {
                    if (git.StdOut.Count != 1)
                        throw new RunProcessException(Resources.Git_UnexpectedOutput, git);
                    return git.StdOut[0];
                }
                throw new RunProcessException(Resources.Git_UnexpectedOutput, git);
            });
        }

        private readonly AsyncCache<string, string> m_Tags = new AsyncCache<string, string>();

        private Task<string> GetTagInternalAsync(string tag)
        {
            return m_Tags.GetSetAsync(tag, async () => {
                RunProcess git = await m_Git.RunFromAsync(m_TopLevelPath, "show-ref", "-s", tag);
                if (git.ExitCode == 1) return string.Empty;
                if (git.ExitCode == 0) {
                    if (git.StdOut.Count != 1)
                        throw new RunProcessException(Resources.Git_UnexpectedOutput, git);
                    return git.StdOut[0];
                }
                throw new RunProcessException(Resources.Git_UnexpectedOutput, git);
            });
        }

        private readonly AsyncCache<(string, string, string), bool> m_Diffs = new AsyncCache<(string, string, string), bool>();

        private Task<bool> GetDiffInternalAsync(string path, string commit1, string commit2)
        {
            return m_Diffs.GetSetAsync((path, commit1, commit2), async () => {
                RunProcess git = await m_Git.RunFromAsync(m_TopLevelPath, "diff", "--quiet", commit1, commit2, "--", path, ":(exclude,glob)**/.editorconfig");
                if (git.ExitCode == 0) return false;
                if (git.ExitCode == 1) return true;
                throw new RunProcessException(Resources.Git_UnexpectedOutput, git);
            });
        }
    }
}
