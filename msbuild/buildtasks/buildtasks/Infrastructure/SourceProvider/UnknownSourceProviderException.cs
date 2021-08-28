namespace RJCP.MSBuildTasks.Infrastructure.SourceProvider
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// The source provider given is unkonwn.
    /// </summary>
    [Serializable]
    internal class UnknownSourceProviderException : SourceProviderException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnknownSourceProviderException"/> class.
        /// </summary>
        public UnknownSourceProviderException()
            : base() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnknownSourceProviderException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public UnknownSourceProviderException(string message)
            : base(message) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnknownSourceProviderException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="provider">The provider that is unknown.</param>
        public UnknownSourceProviderException(string message, string provider)
            : base(message)
        {
            m_Provider = provider;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnknownSourceProviderException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="provider">The provider that is unknown.</param>
        /// <param name="inner">The inner exception to capture with the error.</param>
        public UnknownSourceProviderException(string message, string provider, Exception inner)
            : base(message, inner)
        {
            m_Provider = provider;
        }

        private readonly string m_Provider = string.Empty;

        /// <summary>
        /// Gets the source provider that is unknown.
        /// </summary>
        /// <value>The provider that is unknown.</value>
        public string Provider { get { return m_Provider; } }

        /// <summary>
        /// When overridden in a derived class, sets the <see cref="System.Runtime.Serialization.SerializationInfo"/>
        /// with information about the exception.
        /// </summary>
        /// <param name="info">
        /// The <see cref="SerializationInfo"/> that holds the serialized object data about
        /// the exception being thrown.
        /// </param>
        /// <param name="context">
        /// The <see cref="StreamingContext"/> that contains contextual information about
        /// the source or destination.
        /// </param>
        public override void GetObjectData(SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
        {
            // Serialize our new property, call the base
            info.AddValue("provider", m_Provider);
            base.GetObjectData(info, context);
        }

        /// <summary>
        /// Returns a <see cref="string"/> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="string"/> that represents this instance.</returns>
        public override string ToString()
        {
            if (!string.IsNullOrEmpty(m_Provider)) {
                return $"{base.ToString()}: {m_Provider}";
            } else {
                return base.ToString();
            }
        }
    }
}
