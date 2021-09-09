namespace RJCP.MSBuildTasks
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;
    using Infrastructure.Process;
    using Infrastructure.Tools;
    using Microsoft.Build.Framework;

    /// <summary>
    /// Sign an Assembly using the thumbprint found from a public certificate.
    /// </summary>
    public class X509SignAuthenticode : Microsoft.Build.Utilities.Task
    {
        private StoreName m_StoreName = StoreName.My;
        private StoreLocation m_StoreLocation = StoreLocation.CurrentUser;

        /// <summary>
        /// Define the path where the certificate should be loaded.
        /// </summary>
        [Required]
        public string CertPath { get; set; }

        /// <summary>
        /// Gets or sets the input assembly that should be signed.
        /// </summary>
        /// <value>The input assembly that should be signed.</value>
        [Required]
        public string InputAssembly { get; set; }

        /// <summary>
        /// Gets or sets the time stamp URI that should be used for signing.
        /// </summary>
        /// <value>The time stamp URI that should be used for signing.</value>
        public string TimeStampUri { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the certificate store where to find the signing certificate.
        /// </summary>
        /// <value>The certificate store to find the signing certificate.</value>
        public string CertificateStoreName
        {
            get { return m_StoreName.ToString(); }
            set
            {
                m_StoreName = (StoreName)Enum.Parse(typeof(StoreName), value, true);
            }
        }

        /// <summary>
        /// Gets or sets the certificate location to find the signing certificate.
        /// </summary>
        /// <value>The certificate location to find the signing certificate.</value>
        public string CertificateLocation
        {
            get { return m_StoreLocation.ToString(); }
            set
            {
                m_StoreLocation = (StoreLocation)Enum.Parse(typeof(StoreLocation), value, true);
            }
        }

        /// <summary>
        /// Entry point for the <see cref="ITask"/> to sign an assembly.
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

            if (string.IsNullOrWhiteSpace(InputAssembly)) {
                Log.LogError(Resources.X509_Input_AssemblyNotDefined);
                return false;
            }

            if (!File.Exists(InputAssembly)) {
                Log.LogError(Resources.X509_Input_FileNotFound, InputAssembly);
                return false;
            }

            try {
                Log.LogMessage(Resources.X509_Cert_SignMessage, InputAssembly, CertPath);
                X509Certificate2 pubCert = new X509Certificate2(CertPath);

                Uri timeStampUri = null;
                if (string.IsNullOrWhiteSpace(TimeStampUri)) {
                    Log.LogMessage(Resources.X509_TimeStamp_Ignored);
                } else if (!Uri.TryCreate(TimeStampUri, UriKind.Absolute, out timeStampUri)) {
                    Log.LogWarning(Resources.X509_TimeStamp_Invalid, TimeStampUri);
                } else {
                    Log.LogMessage(Resources.X509_TimeStamp_Found, timeStampUri);
                }

                return SignAsync(pubCert, timeStampUri, m_StoreLocation, m_StoreName, InputAssembly).GetAwaiter().GetResult();
            } catch (Exception ex) {
                Log.LogError(Resources.X509_Cert_SignError,
                    Path.GetFileName(InputAssembly), Path.GetFileName(CertPath), ex.Message);
                return false;
            }
        }

        private async Task<bool> SignAsync(X509Certificate2 signCert, Uri timeStampUri, StoreLocation storeLocation, StoreName storeName, string inputAssembly)
        {
            if (signCert == null) throw new ArgumentNullException(nameof(signCert));
            if (inputAssembly == null) throw new ArgumentNullException(nameof(inputAssembly));

            Executable signTool = await ToolFactory.Instance.GetToolAsync(ToolFactory.SignTool);
            Log.LogMessage(Resources.X509_SignTool_Found, signTool.BinaryPath);

            List<string> signToolArgs = new List<string>() {
                "sign", "/fd", "sha256", "/sha1", signCert.Thumbprint
            };
            if (storeName != StoreName.My)
                signToolArgs.AddRange(new[] { "/s", storeName.ToString() });
            if (storeLocation == StoreLocation.LocalMachine)
                signToolArgs.Add("/sm");
            if (timeStampUri != null)
                signToolArgs.AddRange(new[] { "/tr", timeStampUri.ToString() });
            signToolArgs.Add(inputAssembly);

            RunProcess result = await signTool.RunAsync(signToolArgs.ToArray());
            Log.LogCommandLine(result.Command);
            if (result.ExitCode == 0) {
                Log.LogMessage(Resources.X509_SignTool_Success);
                return true;
            } else {
                Log.LogError(Resources.X509_SignTool_Failure, result.ExitCode);
                foreach (string line in result.StdOut) {
                    Log.LogError($"STDOUT: {line}");
                }
                foreach (string line in result.StdErr) {
                    Log.LogError($"STDERR: {line}");
                }
                return false;
            }
        }
    }
}
