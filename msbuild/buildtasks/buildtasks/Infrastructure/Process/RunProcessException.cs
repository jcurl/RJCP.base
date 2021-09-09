namespace RJCP.MSBuildTasks.Infrastructure.Process
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    [Serializable]
    internal class RunProcessException : Exception
    {
        public RunProcessException()
            : base() { }

        public RunProcessException(string message)
            : base(message) { }

        public RunProcessException(string message, Exception innerException)
            : base(message, innerException) { }

        public RunProcessException(string message, RunProcess process)
            : base(message)
        {
            Initialize(process);
        }

        public RunProcessException(string message, RunProcess process, Exception innerException)
            : base(message, innerException)
        {
            Initialize(process);
        }
        protected RunProcessException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            // Deserialize our new property
            ExitCode = info.GetInt32(nameof(ExitCode));
            Command = info.GetString(nameof(Command));
            WorkingDirectory = info.GetString(nameof(WorkingDirectory));

            // We only record the last line
            StdOut = DeserializeList(info, nameof(StdOut));
            StdErr = DeserializeList(info, nameof(StdErr));
        }

        private void Initialize()
        {
            Command = string.Empty;
            WorkingDirectory = string.Empty;
            StdOut = Array.Empty<string>();
            StdErr = Array.Empty<string>();
        }

        private void Initialize(RunProcess process)
        {
            if (process == null) {
                Initialize();
                return;
            }

            ExitCode = process.ExitCode;
            Command = process.Command ?? string.Empty;
            WorkingDirectory = process.WorkingDirectory ?? string.Empty;
            StdOut = process.StdOut;
            StdErr = process.StdErr;
        }

        public int ExitCode { get; private set; }

        public string Command { get; private set; }

        public string WorkingDirectory { get; private set; }

        public IReadOnlyList<string> StdOut { get; private set; }

        public IReadOnlyList<string> StdErr { get; private set; }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // Serialize our new property, call the base
            info.AddValue(nameof(ExitCode), ExitCode);
            info.AddValue(nameof(Command), Command);
            info.AddValue(nameof(WorkingDirectory), WorkingDirectory);
            SerializeList(info, nameof(StdOut), StdOut);
            SerializeList(info, nameof(StdErr), StdErr);
            base.GetObjectData(info, context);
        }

        private static IReadOnlyList<string> DeserializeList(SerializationInfo info, string name)
        {
            string line = info.GetString(name);
            if (string.IsNullOrWhiteSpace(line))
                return Array.Empty<string>();

            return new List<string>() { line };
        }

        private static void SerializeList(SerializationInfo info, string name, IReadOnlyList<string> list)
        {
            if (list.Count == 0) {
                info.AddValue(name, string.Empty);
                return;
            }

            info.AddValue(name, list[list.Count - 1]);
        }
    }
}
