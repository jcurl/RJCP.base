namespace RJCP.MSBuildTasks.Infrastructure.SourceProvider
{
    /// <summary>
    /// Enum SourceLabel
    /// </summary>
    internal enum SourceLabel
    {
        /// <summary>
        /// The requested commit and the label match.
        /// </summary>
        LabelMatch,

        /// <summary>
        /// The user has provided an override that matches.
        /// </summary>
        LabelOverride,

        /// <summary>
        /// The requested label is not found.
        /// </summary>
        LabelNotFound,

        /// <summary>
        /// The current commit could not be retrieved.
        /// </summary>
        HeadNotFound,

        /// <summary>
        /// The requested commit and the label differ.
        /// </summary>
        LabelDiffers,

        /// <summary>
        /// No label was given.
        /// </summary>
        LabelMissing
    }
}
