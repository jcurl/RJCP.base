namespace RJCP.MSBuildTasks.Infrastructure.SourceProvider
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Manage important information for a path under revision control.
    /// </summary>
    internal interface ISourceControl
    {
        /// <summary>
        /// Gets the type of the revision control.
        /// </summary>
        /// <value>The type of the revision control.</value>
        string RevisionControlType { get; }

        /// <summary>
        /// Gets the current branch for the repository.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>
        /// The name of the branch. This may be <see cref="string.Empty"/> if the current branch is not known, or the
        /// revision control system doesn't support this.
        /// </returns>
        Task<string> GetCurrentBranchAsync(string path);

        /// <summary>
        /// Gets the last commit for the path given.
        /// </summary>
        /// <param name="path">The path to get the commit for.</param>
        /// <returns>The representation for the commit.</returns>
        Task<string> GetCommitAsync(string path);

        /// <summary>
        /// Gets the last commit for the path given in short form.
        /// </summary>
        /// <param name="path">The path to get the commit for.</param>
        /// <returns>The representation for the commit in short form, if available.</returns>
        Task<string> GetCommitShortAsync(string path);

        /// <summary>
        /// Gets the last commit date/time for the path given.
        /// </summary>
        /// <param name="path">The path to get the commit for.</param>
        /// <returns>
        /// The Date/Time in the timezone if possible, for the last commit. Note, that the timezone may have to be
        /// converted to UTC for proper comparison. If no timezone information is available, assume the current
        /// timezone.
        /// </returns>
        Task<DateTime> GetCommitDateTimeAsync(string path);

        /// <summary>
        /// Determines whether the specified path is dirty.
        /// </summary>
        /// <param name="path">The path to check for.</param>
        /// <returns><see langword="true"/> if the specified path is dirty; otherwise, <see langword="false"/>.</returns>
        Task<bool> IsDirtyAsync(string path);

        /// <summary>
        /// Determines whether the specified path is tagged with the tag given.
        /// </summary>
        /// <param name="tag">The tag to check for.</param>
        /// <param name="path">The path to check that has the tag.</param>
        /// <returns>
        /// <see langword="true"/> if the specified tag exists, and the path given matches the contents of the tag;
        /// otherwise, <see langword="false"/>.
        /// </returns>
        Task<SourceLabel> IsTaggedAsync(string tag, string path);
    }
}
