namespace RJCP.MSBuildTasks.Infrastructure.Tools
{
    using System;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using Infrastructure.Process;

    internal class SignToolSimProcess : RunProcess
    {
        private static int SignTool(RunProcess process, string command, string arguments, CancellationToken token)
        {
            SignToolSimProcess signtool = (SignToolSimProcess)process;
            Console.WriteLine($"{signtool.Command}");

            string[] args = Windows.SplitCommandLine(arguments);

            try {
                if (!args[0].Equals("sign", StringComparison.Ordinal))
                    throw new InvalidOperationException($"Unknown signtool command {args[0]}");

                StoreName storeName = FindStoreName(args);
                if (storeName != signtool.ExpectedStoreName)
                    throw new InvalidOperationException($"Incorrect store name found {storeName}");

                StoreLocation storeLocation = FindStoreLocation(args);
                if (storeLocation != signtool.ExpectedStoreLocation)
                    throw new InvalidOperationException($"Incorrect store location found {storeLocation}");

                signtool.LogStdOut("Done Adding Additional Store");

                string thumbPrint = FindThumbPrint(args)
                    ?? throw new InvalidOperationException("No thumbprint found");
                if (!thumbPrint.Equals(signtool.ExpectedThumbPrint, StringComparison.OrdinalIgnoreCase)) {
                    // Thumbprint wasn't found in the current store.
                    signtool.LogStdErr("SignTool Error: No certificates were found that met all the given criteria.");
                    return 1;
                }

                string hashAlg = FindHashAlgorithm(args)
                    ?? throw new InvalidOperationException("No hash algorithm specified");
                if (!hashAlg.Equals("sha256", StringComparison.Ordinal))
                    throw new InvalidOperationException($"Invalid hash algorithm {hashAlg} requested");

                string timeStampUri = FindTimeStampUri(args);
                if (timeStampUri == null && signtool.ExpectedTimeStampUri != null)
                    throw new InvalidOperationException("Expected TimeStampUri but none given");
                if (timeStampUri != null && !timeStampUri.Equals(signtool.ExpectedTimeStampUri))
                    throw new InvalidOperationException("Unexpected TimeStampUri given");

                signtool.LogStdOut(string.Format("Successfully signed: {0}", args[args.Length - 1]));
                return 0;
            } catch (Exception e) {
                Console.WriteLine($"SignTool failed: {e.Message}");
                throw;
            }
        }

        private static string FindHashAlgorithm(string[] args)
        {
            bool hashOption = false;
            foreach (string arg in args) {
                if (hashOption) return arg;
                if (arg.Equals("/fd", StringComparison.Ordinal)) hashOption = true;
            }
            return null;
        }

        private static StoreName FindStoreName(string[] args)
        {
            bool storeOption = false;
            foreach (string arg in args) {
                if (storeOption) return (StoreName)Enum.Parse(typeof(StoreName), arg, true);
                if (arg.Equals("/s", StringComparison.Ordinal)) storeOption = true;
            }
            return StoreName.My;
        }

        private static StoreLocation FindStoreLocation(string[] args)
        {
            foreach (string arg in args) {
                if (arg.Equals("/sm", StringComparison.Ordinal)) return StoreLocation.LocalMachine;
            }
            return StoreLocation.CurrentUser;
        }

        private static string FindThumbPrint(string[] args)
        {
            bool thumbPrint = false;
            foreach (string arg in args) {
                if (thumbPrint) return arg;
                if (arg.Equals("/sha1", StringComparison.Ordinal)) thumbPrint = true;
            }
            return null;
        }

        private static string FindTimeStampUri(string[] args)
        {
            bool timestampOption = false;
            foreach (string arg in args) {
                if (timestampOption) return arg;
                if (arg.Equals("/tr", StringComparison.OrdinalIgnoreCase)) timestampOption = true;
            }
            return null;
        }

        public SignToolSimProcess(string command, string workDir, string arguments)
            : base(SignTool, command, workDir, arguments) { }

        public StoreName ExpectedStoreName { get; set; } = StoreName.My;

        public StoreLocation ExpectedStoreLocation { get; set; } = StoreLocation.CurrentUser;

        public string ExpectedThumbPrint { get; set; } = string.Empty;

        public string ExpectedTimeStampUri { get; set; } = null;
    }
}
