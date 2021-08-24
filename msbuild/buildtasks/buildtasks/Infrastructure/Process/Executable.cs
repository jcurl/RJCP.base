namespace RJCP.MSBuildTasks.Infrastructure.Process
{
    using System;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Infrastructure.Threading.Tasks;

    /// <summary>
    /// An abstract class that others can implement for providing an interface to execute a specific tool.
    /// </summary>
    internal abstract class Executable
    {
        private string m_BinaryPath;
        private bool m_Initialized;
        private readonly AsyncSemaphore m_Semaphore;

        /// <summary>
        /// Initializes a new instance of the <see cref="Executable"/> class.
        /// </summary>
        /// <remarks>
        /// There is no maximum degree of parallelism, so every time a binary is started, it will run.
        /// </remarks>
        protected Executable() : this(0) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Executable"/> class.
        /// </summary>
        /// <param name="maxDegreeOfParallelism">The maximum degree of parallelism.</param>
        /// <remarks>
        /// Setting the maximum degree of parallelism will limit the number of concurrent instances of this particular
        /// executable at once. This could be for example, to limit the number of accesses to disk.
        /// </remarks>
        protected Executable(int maxDegreeOfParallelism)
        {
            // For information on how this works, see
            // https://blogs.msdn.microsoft.com/fkaduk/2018/09/02/multiple-ways-how-to-limit-parallel-tasks-processing/
            if (maxDegreeOfParallelism > 0) {
                m_Semaphore = new AsyncSemaphore(maxDegreeOfParallelism);
            }
        }

        /// <summary>
        /// Execute the binary with the given arguments.
        /// </summary>
        /// <param name="arguments">The arguments to give to the binary.</param>
        /// <returns>The <see cref="RunProcess"/> for obtaining results of the binary.</returns>
        public async Task<RunProcess> RunAsync(params string[] arguments)
        {
            await FindExecutableAsync(true);
            try {
                if (m_Semaphore != null) await m_Semaphore.WaitAsync();
                return await ExecuteProcessAsync(arguments);
            } finally {
                if (m_Semaphore != null) m_Semaphore.Release();
            }
        }

        /// <summary>
        /// Execute the binary with the given arguments.
        /// </summary>
        /// <param name="arguments">The arguments to give to the binary.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns>The <see cref="RunProcess"/> for obtaining results of the binary.</returns>
        public async Task<RunProcess> RunAsync(string[] arguments, CancellationToken token)
        {
            await FindExecutableAsync(true);
            try {
                if (m_Semaphore != null) await m_Semaphore.WaitAsync();
                return await ExecuteProcessAsync(arguments, token);
            } finally {
                if (m_Semaphore != null) m_Semaphore.Release();
            }
        }

        /// <summary>
        /// Execute the binary with the given arguments from a specified working directory.
        /// </summary>
        /// <param name="workDir">The working directory to execute from.</param>
        /// <param name="arguments">The arguments to give to the binary.</param>
        /// <returns>The <see cref="RunProcess"/> for obtaining results of the binary.</returns>
        public async Task<RunProcess> RunFromAsync(string workDir, params string[] arguments)
        {
            await FindExecutableAsync(true);
            try {
                if (m_Semaphore != null) await m_Semaphore.WaitAsync();
                return await ExecuteProcessAsync(workDir, arguments);
            } finally {
                if (m_Semaphore != null) m_Semaphore.Release();
            }
        }

        /// <summary>
        /// Execute the binary with the given arguments from a specified working directory.
        /// </summary>
        /// <param name="workDir">The working directory to execute from.</param>
        /// <param name="arguments">The arguments to give to the binary.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns>The <see cref="RunProcess"/> for obtaining results of the binary.</returns>
        public async Task<RunProcess> RunFromAsync(string workDir, string[] arguments, CancellationToken token)
        {
            await FindExecutableAsync(true);
            try {
                if (m_Semaphore != null) await m_Semaphore.WaitAsync();
                return await ExecuteProcessAsync(workDir, arguments, token);
            } finally {
                if (m_Semaphore != null) m_Semaphore.Release();
            }
        }

        /// <summary>
        /// Executes the process asynchronously.
        /// </summary>
        /// <param name="arguments">The arguments to give to the binary.</param>
        /// <returns>The <see cref="RunProcess"/> for obtaining results of the binary.</returns>
        /// <remarks>This method allows customisation on how the process is executed.</remarks>
        protected virtual Task<RunProcess> ExecuteProcessAsync(params string[] arguments)
        {
            return RunProcess.RunAsync(m_BinaryPath, arguments);
        }

        /// <summary>
        /// Executes the process asynchronously.
        /// </summary>
        /// <param name="arguments">The arguments to give to the binary.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns>The <see cref="RunProcess"/> for obtaining results of the binary.</returns>
        /// <remarks>This method allows customisation on how the process is executed.</remarks>
        protected virtual async Task<RunProcess> ExecuteProcessAsync(string[] arguments, CancellationToken token)
        {
            RunProcess process = new RunProcess(m_BinaryPath, null, arguments);
            await process.ExecuteAsync(false, token);
            return process;
        }

        /// <summary>
        /// Executes the process asynchronously.
        /// </summary>
        /// <param name="workDir">The working directory to execute from.</param>
        /// <param name="arguments">The arguments to give to the binary.</param>
        /// <returns>The <see cref="RunProcess"/> for obtaining results of the binary.</returns>
        /// <remarks>This method allows customisation on how the process is executed.</remarks>
        protected virtual Task<RunProcess> ExecuteProcessAsync(string workDir, string[] arguments)
        {
            return RunProcess.RunFromAsync(m_BinaryPath, workDir, arguments);
        }

        /// <summary>
        /// Executes the process asynchronously.
        /// </summary>
        /// <param name="workDir">The working directory to execute from.</param>
        /// <param name="arguments">The arguments to give to the binary.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns>The <see cref="RunProcess"/> for obtaining results of the binary.</returns>
        /// <remarks>This method allows customisation on how the process is executed.</remarks>
        protected virtual async Task<RunProcess> ExecuteProcessAsync(string workDir, string[] arguments, CancellationToken token)
        {
            RunProcess process = new RunProcess(m_BinaryPath, workDir, arguments);
            await process.ExecuteAsync(false, token);
            return process;
        }

        /// <summary>
        /// Search for the binary to be executed and check that it is valid.
        /// </summary>
        /// <returns>
        /// Returns <see langword="true"/> if the executable could be found as it suitable for use,
        /// <see langword="false"/> otherwise.
        /// </returns>
        public Task<bool> FindExecutableAsync()
        {
            return FindExecutableAsync(false);
        }

        /// <summary>
        /// Search for the binary to be executed and check that it is valid.
        /// </summary>
        /// <param name="throwOnError">
        /// Set to <see langword="true"/> if an exception should be thrown if the tool can't be found, instead of
        /// returning <see langword="false"/>.
        /// </param>
        /// <returns>
        /// Returns <see langword="true"/> if the executable could be found as it suitable for use,
        /// <see langword="false"/> otherwise.
        /// </returns>
        /// <exception cref="InvalidOperationException">The tool cannot be found.</exception>
        public async Task<bool> FindExecutableAsync(bool throwOnError)
        {
            if (m_Initialized) {
                if (throwOnError && m_BinaryPath == null)
                    throw new InvalidOperationException(ErrorToolNotAvailable);
                return m_BinaryPath != null;
            }
            await Initialize();
            if (throwOnError && m_BinaryPath == null)
                throw new InvalidOperationException(ErrorToolNotAvailable);
            return m_BinaryPath != null;
        }

        /// <summary>
        /// Gets a value indicating whether this instance is available.
        /// </summary>
        /// <value>
        /// Returns <see langword="true"/> if this instance is available; otherwise, <see langword="false"/>.
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// A call must be made prior to <see cref="FindExecutableAsync()"/>.
        /// </exception>
        public bool IsAvailable
        {
            get
            {
                if (!m_Initialized)
                    throw new InvalidOperationException(Resources.Infra_Process_ExeNotFound);
                return m_BinaryPath != null;
            }
        }

        /// <summary>
        /// Gets the path to the executable binary.
        /// </summary>
        /// <value>The path to the executable binary.</value>
        /// <exception cref="InvalidOperationException">
        /// A call must be made prior to <see cref="FindExecutableAsync()"/>.
        /// </exception>
        public string BinaryPath
        {
            get
            {
                if (!m_Initialized)
                    throw new InvalidOperationException(Resources.Infra_Process_ExeNotFound);
                return m_BinaryPath;
            }
        }

        private async Task Initialize()
        {
            try {
                m_BinaryPath = await InitializeAsync();
            } catch (Exception) {
                // An exception was raised when initializing. We don't know what this exception is, except that it
                // can't be initialized.
                m_BinaryPath = null;
            } finally {
                m_Initialized = true;
            }
        }

        /// <summary>
        /// Gets the localized string to indicate that the tool is not available.
        /// </summary>
        /// <value>The localized string to indicate that the tool is not available.</value>
        protected abstract string ErrorToolNotAvailable { get; }

        /// <summary>
        /// Performs initialization, looking for the tool and checking that it is compatible. If it is not compatible
        /// </summary>
        /// <returns>The path to the binary, if it is valid and usable.</returns>
        /// <remarks>
        /// This method should search for the binary and return a string that contains the path to the binary. It
        /// should already check that the path is valid, and possibly run the executable to ensure that it reports the
        /// correct version.
        /// <para>
        /// If the executable cannot be found, or it is deemed unusable, an exception may be be thrown, or the
        /// <see langword="null"/> string can be returned.
        /// </para>
        /// <para>When testing the binary, use the <see cref="RunProcess"/> static methods direct.</para>
        /// </remarks>
        protected abstract Task<string> InitializeAsync();

        private static readonly ConcurrentDictionary<string, AsyncValue<bool>> s_Paths =
            new ConcurrentDictionary<string, AsyncValue<bool>>();

        private sealed class FilePathEnumerable : IEnumerable<string>
        {
            private readonly string m_Binary;

            public FilePathEnumerable(string binary)
            {
                m_Binary = binary;
            }


            public IEnumerator<string> GetEnumerator()
            {
                return new FileEnumerator(this);
            }

            IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

            private sealed class FileEnumerator : IEnumerator<string>
            {
                private int m_PathIndex = -1;
                private readonly string[] m_Paths;

                public FileEnumerator(FilePathEnumerable parent)
                {
                    string pathVar = Environment.GetEnvironmentVariable("PATH");
                    if (string.IsNullOrWhiteSpace(pathVar)) {
                        m_Paths = Array.Empty<string>();
                    } else {
                        string[] paths = pathVar.Split(Path.PathSeparator);
                        m_Paths = new string[paths.Length];
                        for (int i = 0; i < paths.Length; i++) {
                            m_Paths[i] = Path.GetFullPath(Path.Combine(paths[i], parent.m_Binary));
                        }
                    }
                }

                public string Current
                {
                    get
                    {
                        if (m_PathIndex < 0 || m_PathIndex >= m_Paths.Length)
                            throw new InvalidOperationException("Enumeration out of bounds");
                        return m_Paths[m_PathIndex];
                    }
                }

                object IEnumerator.Current { get { return Current; } }

                public bool MoveNext()
                {
                    while (true) {
                        if (m_PathIndex + 1 == m_Paths.Length) return false;
                        m_PathIndex++;

                        if (CheckFileExists(m_Paths[m_PathIndex])) return true;
                    }
                }

                public void Reset()
                {
                    m_PathIndex = -1;
                }

                public void Dispose()
                {
                    // Nothing to dispose
                }
            }
        }

        /// <summary>
        /// Gets an enumerable that can be used to check for files that exist.
        /// </summary>
        /// <param name="binary">The binary file name that is being sought for.</param>
        /// <returns>A string enumerable of full paths from the PATH environment variable where the file exists.</returns>
        protected static IEnumerable<string> FindFiles(string binary)
        {
            return new FilePathEnumerable(binary);
        }

        /// <summary>
        /// Check if the path to a binary executable exists.
        /// </summary>
        /// <param name="path">The path to the binary executable to check.</param>
        /// <returns>Returns <see langword="true"/> if the file exists, <see langword="false"/> otherwise.</returns>
        /// <remarks>
        /// Paths that have been searched (for this instance of the class) are cached.
        /// </remarks>
        protected static async Task<bool> CheckFileExistsAsync(string path)
        {
            path = Path.GetFullPath(path);
            AsyncValue<bool> pathFound;

            // See if we've already started searching for this path. The loop might be that another thread is also
            // searching for the path, and so we want the same instance in both that we don't search twice in parallel.
            while (true) {
                if (s_Paths.TryGetValue(path, out pathFound)) break;

                pathFound = new AsyncValue<bool>();
                if (s_Paths.TryAdd(path, pathFound)) break;
            }

            // If we're already looking for this path, it just waits, else it will run on a background thread to check
            // for the existence of the file.
            return await pathFound.GetSetAsync(() => {
                return Task.Run(() => {
                    if (Directory.Exists(path)) return false;
                    return File.Exists(path);
                });
            });
        }

        /// <summary>
        /// Check if the path to a binary executable exists.
        /// </summary>
        /// <param name="path">The path to the binary executable to check.</param>
        /// <returns>Returns <see langword="true"/> if the file exists, <see langword="false"/> otherwise.</returns>
        /// <remarks>
        /// Paths that have been searched (for this instance of the class) are cached.
        /// </remarks>
        protected static bool CheckFileExists(string path)
        {
            AsyncValue<bool> pathFound;

            // See if we've already started searching for this path. The loop might be that another thread is also
            // searching for the path, and so we want the same instance in both that we don't search twice in parallel.
            while (true) {
                if (s_Paths.TryGetValue(path, out pathFound)) break;

                pathFound = new AsyncValue<bool>();
                if (s_Paths.TryAdd(path, pathFound)) break;
            }

            return pathFound.GetSet(() => {
                if (Directory.Exists(path)) return false;
                return File.Exists(path);
            });
        }
    }
}
