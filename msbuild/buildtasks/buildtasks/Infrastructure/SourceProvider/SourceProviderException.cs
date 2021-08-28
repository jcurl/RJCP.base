namespace RJCP.MSBuildTasks.Infrastructure.SourceProvider
{
    using System;

    /// <summary>
    /// Base exception for source provider exceptions.
    /// </summary>
    [Serializable]
    internal abstract class SourceProviderException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SourceProviderException"/> class.
        /// </summary>
        protected SourceProviderException()
            : base() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SourceProviderException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        protected SourceProviderException(string message)
            : base(message) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SourceProviderException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="inner">The inner exception to capture with the error.</param>
        protected SourceProviderException(string message, Exception inner)
            : base(message, inner) { }
    }
}
