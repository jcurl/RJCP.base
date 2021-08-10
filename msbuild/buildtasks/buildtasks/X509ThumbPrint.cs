namespace RJCP.MSBuildTasks
{
    using System;
    using System.IO;
    using System.Security.Cryptography.X509Certificates;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    /// <summary>
    /// The X509ThumbPrint Task.
    /// </summary>
    public class X509ThumbPrint : Microsoft.Build.Utilities.Task
    {
        /// <summary>
        /// Define the path where the certificate should be loaded.
        /// </summary>
        [Required]
        public string CertPath { get; set; }

        /// <summary>
        /// Outputs the thumbprint if the certificate <see cref="CertPath"/>.
        /// </summary>
        /// <value>
        /// The SHA1 thumbprint of the certificate <see cref="CertPath"/> after the <see
        /// cref="Execute"/> method is called.
        /// </value>
        [Output]
        public string ThumbPrint { get; private set; } = string.Empty;

        /// <summary>
        /// Entry point for the <see cref="ITask"/> to get the certificate thumbprint.
        /// </summary>
        /// <returns>
        /// Returns <see langword="true"/> if successful, <see langword="false"/> if the task failed.
        /// </returns>
        public override bool Execute()
        {
            if (string.IsNullOrWhiteSpace(CertPath)) {
                Log.LogError(Resources.X509_Cert_CertPathNotDefined);
                return false;
            }

            if (!File.Exists(CertPath)) {
                Log.LogError(Resources.X509_Cert_FileNotFound, CertPath);
                return false;
            }

            try {
                Log.LogMessage(Resources.X509_Cert_Found, CertPath);
                ThumbPrint = GetX509Thumbprint(CertPath);
                return true;
            } catch (Exception ex) {
                Log.LogError(Resources.X509_Cert_FileInvalid,
                    Path.GetFileName(CertPath), ex.Message);
                return false;
            }
        }

        private static string GetX509Thumbprint(string certFile)
        {
            if (certFile == null) throw new ArgumentNullException(nameof(certFile));
            if (string.IsNullOrEmpty(certFile))
                throw new ArgumentException(Resources.X509_Cert_ArgumentNull, nameof(certFile));

            X509Certificate2 cert = new X509Certificate2(certFile);
            return cert.Thumbprint;
        }
    }
}
