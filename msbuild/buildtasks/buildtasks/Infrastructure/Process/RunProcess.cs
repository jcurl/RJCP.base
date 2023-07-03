namespace RJCP.MSBuildTasks.Infrastructure.Process
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Infrastructure.Process.Internal;

    /// <summary>
    /// Wrapper class to run a process, capturing output and logging to a global source "RjBuild.Process" if otherwise
    /// not specified.
    /// </summary>
    /// <remarks>
    /// The <see cref="RunProcess"/> is intended for executing text based processes that usually run for a short period
    /// of time and then exit. It caches the outputs (as text) into memory as it arrives, making this class useful for
    /// starting external processes like scripts.
    /// <para>
    /// One can derive from this class for specialized executables (although aggregation is probably mroe flexible), and
    /// also for creating simulations (useful for running commands using C# code on a thread, instead of an actual
    /// process), which can assist in unit testing (even on multiple platforms) as no processes are actaully run and
    /// output is simulated.
    /// </para>
    /// </remarks>
    internal class RunProcess
    {
        /// <summary>
        /// Delegate used for simulating running processes for testing purposes.
        /// </summary>
        /// <param name="sender">The process object calling this action.</param>
        /// <param name="command">The command to execute.</param>
        /// <param name="arguments">The arguments to give to the command.</param>
        /// <param name="token">The token that can be used to cancel the simulation.</param>
        /// <returns>System.Int32.</returns>
        protected internal delegate int SimAction(RunProcess sender, string command, string arguments, CancellationToken token);

        #region Static Methods for Running Executables
        /// <summary>
        /// Runs the specified command.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        /// <param name="arguments">The arguments to give to the command.</param>
        /// <returns>An object to get the results of the command.</returns>
        public static RunProcess Run(string command, params string[] arguments)
        {
            return RunFrom(command, null, arguments);
        }

        /// <summary>
        /// Runs the specified command from a specific working directory
        /// </summary>
        /// <param name="command">The command to execute.</param>
        /// <param name="workDir">The directory to execute the command from.</param>
        /// <param name="arguments">The arguments to give to the command.</param>
        /// <returns>An object to get the results of the command.</returns>
        public static RunProcess RunFrom(string command, string workDir, params string[] arguments)
        {
            RunProcess process = new RunProcess(command, workDir, arguments);
            process.Execute();
            return process;
        }

        /// <summary>
        /// Runs the specified command.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        /// <param name="arguments">The arguments to give to the command.</param>
        /// <returns>An object to get the results of the command.</returns>
        public static Task<RunProcess> RunAsync(string command, params string[] arguments)
        {
            return RunFromAsync(command, null, arguments);
        }

        /// <summary>
        /// Runs the specified command.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        /// <param name="arguments">The arguments to give to the command.</param>
        /// <param name="token">The cancellation token which will attempt to terminate the process.</param>
        /// <returns>An object to get the results of the command.</returns>
        public static Task<RunProcess> RunAsync(string command, string[] arguments, CancellationToken token)
        {
            return RunFromAsync(command, null, arguments, token);
        }

        /// <summary>
        /// Runs the specified command from a specific working directory
        /// </summary>
        /// <param name="command">The command to execute.</param>
        /// <param name="workDir">The directory to execute the command from.</param>
        /// <param name="arguments">The arguments to give to the command.</param>
        /// <returns>An object to get the results of the command.</returns>
        public static async Task<RunProcess> RunFromAsync(string command, string workDir, params string[] arguments)
        {
            RunProcess process = new RunProcess(command, workDir, arguments);
            await process.ExecuteAsync();
            return process;
        }

        /// <summary>
        /// Runs the specified command from a specific working directory
        /// </summary>
        /// <param name="command">The command to execute.</param>
        /// <param name="workDir">The directory to execute the command from.</param>
        /// <param name="arguments">The arguments to give to the command.</param>
        /// <param name="token">The cancellation token which will attempt to terminate the process.</param>
        /// <returns>An object to get the results of the command.</returns>
        public static async Task<RunProcess> RunFromAsync(string command, string workDir, string[] arguments, CancellationToken token)
        {
            RunProcess process = new RunProcess(command, workDir, arguments);
            await process.ExecuteAsync(token);
            return process;
        }

        /// <summary>
        /// Algorithms for process handling specific to Windows
        /// </summary>
        public static class Windows
        {
            /// <summary>
            /// Splits the command line into its consitituents, the same was that MSVC does it.
            /// </summary>
            /// <param name="arguments">The arguments string that needs to be split.</param>
            /// <returns>The list of arguments</returns>
            /// <remarks>
            /// This method works for the arguments to the <see cref="System.Diagnostics.Process"/> class when creating
            /// when the command is not empty or null. That is, it splits correction assuming this is the second
            /// argument and following (assuming the first argument is the process that you wish to execute).
            /// <para>It has been tested to match the API call <c>CommandLineToArgvW</c> for its parsing.</para>
            /// <para>
            /// For more information, refer to the web site
            /// http://www.windowsinspired.com/how-a-windows-programs-splits-its-command-line-into-individual-arguments/
            /// </para>
            /// </remarks>
            public static string[] SplitCommandLine(string arguments)
            {
                if (string.IsNullOrWhiteSpace(arguments))
                    return Array.Empty<string>();

                StringBuilder arg = new StringBuilder();
                List<string> args = new List<string>();
                bool special = false;
                bool escape = false;
                bool quote = false;
                int backslashcount = 0;
                foreach (char c in arguments) {
                    if (!escape) {
                        if (c == '\"') {
                            if (!quote) {
                                quote = true;
                            } else {
                                if (special) {
                                    quote = false;
                                    arg.Append('\"');
                                }
                                special = !special;
                            }
                            continue;
                        }
                        if (quote) {
                            quote = false;
                            special = !special;
                        }

                        if (c == ' ' || c == '\t') {
                            if (!special) {
                                if (arg.Length != 0) {
                                    args.Add(arg.ToString());
                                    arg.Clear();
                                }
                            } else {
                                arg.Append(' ');
                            }
                            continue;
                        }
                        if (c == '\\') {
                            escape = true;
                            backslashcount = 1;
                            continue;
                        }
                        arg.Append(c);
                    } else {
                        if (c == '\\') {
                            backslashcount++;
                            continue;
                        }
                        if (c == '\"') {
                            arg.Append('\\', backslashcount / 2);
                            if (backslashcount % 2 == 0) {
                                special = !special;
                            } else {
                                arg.Append('\"');
                            }
                            escape = false;
                            continue;
                        }
                        arg.Append('\\', backslashcount);
                        if (c >= 1 && c <= 32) {
                            if (!special) {
                                if (arg.Length != 0) {
                                    args.Add(arg.ToString());
                                    arg.Clear();
                                }
                            } else {
                                arg.Append(' ');
                            }
                        } else {
                            arg.Append(c);
                        }
                        escape = false;
                    }
                }

                if (escape)
                    arg.Append('\\', backslashcount);
                if (arg.Length != 0) args.Add(arg.ToString());

                return args.ToArray();
            }

            public static string JoinCommandLine(params string[] arguments)
            {
                StringBuilder cmdLine = new StringBuilder();
                StringBuilder escArg = new StringBuilder();
                foreach (string arg in arguments) {
                    bool quote = false;
                    foreach (char c in arg) {
                        if (c == ' ' || c == '\t') {
                            quote = true;
                        } else if (c == '\"') {
                            escArg.Append('\\');
                        }
                        escArg.Append(c);
                    }
                    if (cmdLine.Length > 0) cmdLine.Append(' ');
                    if (quote) {
                        if (arg[arg.Length - 1] == '\\')
                            escArg.Append('\\');
                        cmdLine.Append('"').Append(escArg).Append('"');
                    } else {
                        cmdLine.Append(escArg);
                    }
                    escArg.Clear();
                }
                return cmdLine.ToString();
            }
        }
        #endregion

        #region Construction
        private enum KillCommand
        {
            None,
            Terminated,
            Cancelled
        }

        private enum RunningState
        {
            None,
            Running,
            Completed
        }

        private string m_Command;
        private string m_Arguments;
        private readonly SimAction m_Simulation;
        private IProcessWorker m_ProcessWorker;
        private RunningState m_RunningState;
        private readonly List<string> m_StdOut = new List<string>();
        private readonly List<string> m_StdErr = new List<string>();
        private KillCommand m_Killed;
        private bool m_IsExited;
        private int m_ExitCode;

        /// <summary>
        /// Initializes a new instance of the <see cref="RunProcess"/> class to run a simulation.
        /// </summary>
        /// <param name="simulation">The simulation function which returns the exit code.</param>
        /// <param name="command">The command to execute.</param>
        /// <param name="workDir">The directory to execute the command from.</param>
        /// <param name="arguments">The arguments to give to the command.</param>
        /// <remarks>
        /// This protected method allows you to derive from this class to create what looks like a <see
        /// cref="RunProcess"/> and do some work without actually starting a process. Instead the <paramref
        /// name="simulation"/> is run as a new thread, thus providing the possibility to simulate a process.
        /// <para>
        /// When it is then executed, the simulation delegate is called which contains the reference to your object, the
        /// command, working directory and arguments (as a single string, which is the same format that the <see
        /// cref="System.Diagnostics.Process"/> gets). The delegate can then call the methods to log.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code lang="csharp">
        /// <![CDATA[
        /// internal class GetDirSimProcess : RunProcess
        /// {
        ///     private static int GetDirSim(RunProcess process, string command, string arguments, CancellationToken token)
        ///     {
        ///         GetDirSimProcess p = (GetDirSimProcess)process;
        ///
        ///         p.LogStdOut(" Volume in drive C has no label.");
        ///         p.LogStdOut(" Volume Serial Number is 3A2G-7Z2W");
        ///         p.LogStdOut(string.Empty);
        ///         p.LogStdOut($" Directory of {p.WorkingDirectory}");
        ///         p.LogStdOut(string.Empty);
        ///         p.LogStdOut("06/08/2021  18:31    <DIR>          .");
        ///         p.LogStdOut("06/08/2021  18:31    <DIR>          ..");
        ///         p.LogStdOut("25/05/2021  17:59            29,048 testhost.dll");
        ///         p.LogStdOut("25/05/2021  18:00           149,360 testhost.exe");
        ///         p.LogStdOut("              49 File(s)      6,918,578 bytes");
        ///         p.LogStdOut("              17 Dir(s)  830,622,584,832 bytes free");
        ///         return 0;
        ///     }
        ///
        ///     public GetDirSimProcess(string command, string workDir, string arguments)
        ///         : base(GetDirSim, command, workDir, arguments) { }
        /// }
        ///
        /// string currentDir = Environment.CurrentDirectory;
        /// RunProcess process = new GetDirSimProcess("cmd", currentDir, "/c dir");
        /// process.Execute();
        /// ]]>
        /// </code>
        /// </example>
        protected RunProcess(SimAction simulation, string command, string workDir, string arguments)
        {
            if (simulation == null)
                throw new ArgumentNullException(nameof(simulation));

            Initialize(command, workDir, arguments);
            m_Simulation = simulation;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RunProcess"/> class to run a specific process.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        /// <param name="workDir">The directory to execute the command from.</param>
        /// <param name="arguments">The arguments to give to the command.</param>
        /// <remarks>
        /// This method takes <paramref name="arguments"/> and concatenates them together before starting the process.
        /// The concatenation of the arguments is Windows specific and may not work properly, especially with
        /// backslashes in the arguments at the end of the argument.
        /// </remarks>
        public RunProcess(string command, string workDir, string[] arguments)
        {
            Initialize(command, workDir, Windows.JoinCommandLine(arguments));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RunProcess"/> class to run a specific process.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        /// <param name="workDir">The directory to execute the command from.</param>
        /// <param name="arguments">The arguments to give to the command.</param>
        public RunProcess(string command, string workDir, string arguments)
        {
            Initialize(command, workDir, arguments);
        }

        private void Initialize(string command, string workDir, string arguments)
        {
            m_Command = command;
            WorkingDirectory = workDir;
            m_Arguments = arguments;

            // The command is only used for pretty printing. It isn't actually interpreted.
            Command = string.Format(CultureInfo.InvariantCulture, "{0} {1}", Windows.JoinCommandLine(m_Command), m_Arguments);
        }
        #endregion

        private readonly object m_ProcessLock = new object();

        private IProcessWorker GetProcessWorker(string command, string workingDir, string arguments)
        {
            if (m_Simulation != null) return new ProcessSim(this, m_Simulation, command, arguments);
            return new ProcessWorker(command, workingDir, arguments);
        }

        /// <summary>
        /// Executes the process as instantiated by this instance.
        /// </summary>
        /// <exception cref="InvalidOperationException">The process is or was already running.</exception>
        public void Execute()
        {
            lock (m_ProcessLock) {
                if (m_RunningState != RunningState.None)
                    throw new InvalidOperationException(Resources.Infra_Process_RunProcess_ExecuteTwice);
                m_RunningState = RunningState.Running;

                m_ProcessWorker = GetProcessWorker(m_Command, WorkingDirectory, m_Arguments);
                m_ProcessWorker.OutputDataReceived += ProcessWorker_OutputDataReceived;
                m_ProcessWorker.ErrorDataReceived += ProcessWorker_ErrorDataReceived;
                m_ProcessWorker.Start();
            }
            m_ProcessWorker.Wait(Timeout.Infinite);
            OnProcessExit(m_ProcessWorker.ExitCode);
        }

        /// <summary>
        /// Execute the process as an asynchronous operation.
        /// </summary>
        /// <returns>A Task representing the asynchronous operation.</returns>
        /// <exception cref="InvalidOperationException">The process is or was already running.</exception>
        public async Task ExecuteAsync()
        {
            Task executeTask;
            lock (m_ProcessLock) {
                if (m_RunningState != RunningState.None)
                    throw new InvalidOperationException(Resources.Infra_Process_RunProcess_ExecuteTwice);
                m_RunningState = RunningState.Running;

                if (m_Killed != KillCommand.None) {
                    // Terminate was called prior, so skip. It can't have been terminated because of a token.
                    ExitCode = -1;
                    executeTask = Task.FromResult<object>(null);
                } else {
                    TaskCompletionSource<object> executeTaskSource = new TaskCompletionSource<object>();
                    m_ProcessWorker = GetProcessWorker(m_Command, WorkingDirectory, m_Arguments);
                    m_ProcessWorker.OutputDataReceived += ProcessWorker_OutputDataReceived;
                    m_ProcessWorker.ErrorDataReceived += ProcessWorker_ErrorDataReceived;
                    m_ProcessWorker.ProcessExitEvent += (s, e) => {
                        OnProcessExit(e.Result);
                        executeTaskSource.SetResult(null);
                    };
                    m_ProcessWorker.Start();
                    executeTask = executeTaskSource.Task;
                }
            }
            await executeTask;
        }

        /// <summary>
        /// Execute the process as an asynchronous operation.
        /// </summary>
        /// <param name="token">The cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        public Task ExecuteAsync(CancellationToken token)
        {
            return ExecuteAsync(true, token);
        }

        /// <summary>
        /// Execute the task asynchronously.
        /// </summary>
        /// <param name="throwOnCancel">
        /// Set to <see langword="true"/> if canceling the token should raise a
        /// <see cref="OperationCanceledException"/>, otherwise if <see langword="false"/>, just terminates the process.
        /// </param>
        /// <param name="token">The cancellation token to use to terminate the process executed.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        /// <exception cref="InvalidOperationException">Cannot execute this process twice.</exception>
        /// <exception cref="OperationCanceledException">The operation has been canceled by the token.</exception>
        /// <remarks>
        /// Set <paramref name="throwOnCancel"/> to <see langword="true"/> if <paramref name="token"/> should result in
        /// the exception <see cref="OperationCanceledException"/> being thrown. Set to <see langword="false"/> if
        /// canceling the token should be equivalent to just terminating the running process. You may want to set this
        /// to <see langword="false"/> if the process needs to be canceled, but the <see cref="StdOut"/> or
        /// <see cref="StdErr"/> are still required. In this case the <see cref="ExitCode"/> is set to -1.
        /// </remarks>
        public async Task ExecuteAsync(bool throwOnCancel, CancellationToken token)
        {
            Task executeTask;
            lock (m_ProcessLock) {
                if (m_RunningState != RunningState.None)
                    throw new InvalidOperationException(Resources.Infra_Process_RunProcess_ExecuteTwice);
                m_RunningState = RunningState.Running;

                TaskCompletionSource<object> executeTaskSource = new TaskCompletionSource<object>();
                token.Register(() => {
                    // This callback can be called as soon as it's registered. Will result in Terminating, but because
                    // the process hasn't started, it will only set the m_Killed variable.
                    Terminate(KillCommand.Cancelled);
                });

                switch (m_Killed) {
                case KillCommand.Terminated:
                    ExitCode = -1;
                    executeTask = Task.FromResult<object>(null);
                    break;
                case KillCommand.Cancelled:
                    ExitCode = -1;
                    if (throwOnCancel) {
                        executeTask = Task.FromCanceled(token);
                    } else {
                        executeTask = Task.FromResult<object>(null);
                    }
                    break;
                default:
                    m_ProcessWorker = GetProcessWorker(m_Command, WorkingDirectory, m_Arguments);
                    m_ProcessWorker.OutputDataReceived += ProcessWorker_OutputDataReceived;
                    m_ProcessWorker.ErrorDataReceived += ProcessWorker_ErrorDataReceived;
                    m_ProcessWorker.ProcessExitEvent += (s, e) => {
                        OnProcessExit(e.Result);
                        switch (m_Killed) {
                        case KillCommand.None:
                        case KillCommand.Terminated:
                            executeTaskSource.TrySetResult(null);
                            break;
                        case KillCommand.Cancelled:
                            if (throwOnCancel) {
                                executeTaskSource.TrySetCanceled();
                            } else {
                                executeTaskSource.TrySetResult(null);
                            }
                            break;
                        }
                    };
                    m_ProcessWorker.Start();
                    executeTask = executeTaskSource.Task;
                    break;
                }
            }
            await executeTask;
        }

        private sealed class ProcessAsyncResult : IO.AsyncResultNoResult
        {
            public ProcessAsyncResult(AsyncCallback asyncCallback, object state, object owner, string operationId)
                : base(asyncCallback, state, owner, operationId) { }
        }

        private ProcessAsyncResult m_ExecuteAsyncResult;

        /// <summary>
        /// Begins execution of the process asynchronously.
        /// </summary>
        /// <param name="asyncCallback">The asynchronous callback.</param>
        /// <param name="state">The state.</param>
        /// <returns>An asynchronous object for state.</returns>
        /// <exception cref="InvalidOperationException">Cannot execute this process twice.</exception>
        public IAsyncResult BeginExecute(AsyncCallback asyncCallback, object state)
        {
            lock (m_ProcessLock) {
                if (m_RunningState != RunningState.None)
                    throw new InvalidOperationException(Resources.Infra_Process_RunProcess_ExecuteTwice);
                m_RunningState = RunningState.Running;

                if (m_Killed != KillCommand.None) {
                    ExitCode = -1;
                    m_ExecuteAsyncResult = new ProcessAsyncResult(asyncCallback, state, this, "Execute");
                    m_ExecuteAsyncResult.Complete(null, true);
                } else {
                    m_ExecuteAsyncResult = new ProcessAsyncResult(asyncCallback, state, this, "Execute");
                    m_ProcessWorker = GetProcessWorker(m_Command, WorkingDirectory, m_Arguments);
                    m_ProcessWorker.OutputDataReceived += ProcessWorker_OutputDataReceived;
                    m_ProcessWorker.ErrorDataReceived += ProcessWorker_ErrorDataReceived;
                    m_ProcessWorker.ProcessExitEvent += (s, e) => {
                        OnProcessExit(e.Result);
                        m_ExecuteAsyncResult.Complete(null, false);
                    };
                    m_ProcessWorker.Start();
                }
                return m_ExecuteAsyncResult;
            }
        }

        /// <summary>
        /// Ends the asynchronous process execution started from <see cref="BeginExecute(AsyncCallback, object)"/>.
        /// </summary>
        /// <param name="result">
        /// The asynchronous object as returned by <see cref="BeginExecute(AsyncCallback, object)"/>.
        /// </param>
        public void EndExecute(IAsyncResult result)
        {
            ProcessAsyncResult.End(result, this, "Execute");
        }

        private void OnProcessExit(int exitCode)
        {
            ExitCode = exitCode;
            OnProcessExitEvent(this, new ProcessExitedEventArgs(ExitCode));
            m_ProcessWorker.Dispose();
        }

        /// <summary>
        /// Occurs when the process sends text on the output.
        /// </summary>
        public event EventHandler<ConsoleDataEventArgs> OutputDataReceived;

        /// <summary>
        /// Occurs when the process sends error text on the output.
        /// </summary>
        public event EventHandler<ConsoleDataEventArgs> ErrorDataReceived;

        /// <summary>
        /// Occurs when the process is finished.
        /// </summary>
        public event EventHandler<ProcessExitedEventArgs> ProcessExitEvent;

        private void ProcessWorker_OutputDataReceived(object sender, ConsoleDataEventArgs e)
        {
            if (e.Data != null) {
                OnOutputDataReceived(sender, e);
                if (e.Data != null) m_StdOut.Add(e.Data);
            }
        }

        /// <summary>
        /// Derived classes (especially for simulation) can use this to output to the standard output.
        /// </summary>
        /// <param name="line">The line to push to the standard output.</param>
        protected void LogStdOut(string line)
        {
            ProcessWorker_OutputDataReceived(this, new ConsoleDataEventArgs(line));
        }

        /// <summary>
        /// Handles the <see cref="OutputDataReceived" /> event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="ConsoleDataEventArgs"/> instance containing the event data.</param>
        protected virtual void OnOutputDataReceived(object sender, ConsoleDataEventArgs e)
        {
            EventHandler<ConsoleDataEventArgs> handler = OutputDataReceived;
            if (handler != null) handler(sender, e);
        }

        private void ProcessWorker_ErrorDataReceived(object sender, ConsoleDataEventArgs e)
        {
            if (e.Data != null) {
                OnErrorDataReceived(sender, e);
                if (e.Data != null) m_StdErr.Add(e.Data);
            }
        }

        /// <summary>
        /// Derived classes (especially for simulation) can use this to output to the standard output.
        /// </summary>
        /// <param name="line">The line.</param>
        protected void LogStdErr(string line)
        {
            ProcessWorker_ErrorDataReceived(this, new ConsoleDataEventArgs(line));
        }

        /// <summary>
        /// Handles the <see cref="ErrorDataReceived"/> event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="ConsoleDataEventArgs"/> instance containing the event data.</param>
        protected virtual void OnErrorDataReceived(object sender, ConsoleDataEventArgs e)
        {
            EventHandler<ConsoleDataEventArgs> handler = ErrorDataReceived;
            if (handler != null) handler(sender, e);
        }

        /// <summary>
        /// Handles the <see cref="ProcessExitEvent"/> event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="ProcessExitedEventArgs"/> instance containing the event data.</param>
        protected virtual void OnProcessExitEvent(object sender, ProcessExitedEventArgs e)
        {
            EventHandler<ProcessExitedEventArgs> handler = ProcessExitEvent;
            if (handler != null) handler(sender, e);
        }

        /// <summary>
        /// Gets the standard out.
        /// </summary>
        /// <value>The standard out.</value>
        public IReadOnlyList<string> StdOut
        {
            get { return new ReadOnlyCollection<string>(m_StdOut); }
        }

        /// <summary>
        /// Gets the standard error.
        /// </summary>
        /// <value>The standard error.</value>
        public IReadOnlyList<string> StdErr
        {
            get { return new ReadOnlyCollection<string>(m_StdErr); }
        }

        /// <summary>
        /// Gets the exit code.
        /// </summary>
        /// <value>The exit code.</value>
        public int ExitCode
        {
            get
            {
                if (!m_IsExited)
                    throw new InvalidOperationException(Resources.Infra_Process_NotComplete);
                return m_ExitCode;
            }
            private set
            {
                m_IsExited = true;
                m_ExitCode = value;
            }
        }

        /// <summary>
        /// Gets the command string that was run.
        /// </summary>
        /// <value>The command string that was run.</value>
        public string Command { get; protected set; }

        /// <summary>
        /// Gets the working directory where the command was run.
        /// </summary>
        /// <value>The working directory where the command was run.</value>
        public string WorkingDirectory { get; protected set; }

        /// <summary>
        /// Terminates the running process.
        /// </summary>
        public virtual void Terminate()
        {
            Terminate(KillCommand.Terminated);
        }

        private void Terminate(KillCommand reason)
        {
            lock (m_ProcessLock) {
                if (m_Killed != KillCommand.None) return;
                m_Killed = reason;

                if (m_ProcessWorker != null)
                    m_ProcessWorker.Terminate();
            }
        }
    }
}
