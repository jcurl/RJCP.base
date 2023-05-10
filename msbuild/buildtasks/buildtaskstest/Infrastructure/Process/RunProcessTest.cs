namespace RJCP.MSBuildTasks.Infrastructure.Process
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;

    public class RunProcessTest
    {
        [Test]
        [Platform(Include = "Win32")]
        public void ProcessRunStatic()
        {
            RunProcess process = RunProcess.Run("cmd", "/c", "dir");
            Assert.That(process.ExitCode, Is.EqualTo(0));
            Assert.That(process.StdOut, Is.Not.Empty);
            Assert.That(process.Command, Is.EqualTo("cmd /c dir"));

            // Dump the contents for evaluation later
            foreach (string line in process.StdOut) {
                Console.WriteLine("stdout: {0}", line);
            }
            foreach (string line in process.StdErr) {
                Console.WriteLine("stderr: {0}", line);
            }
        }

        [Test]
        [Platform(Include = "Win32")]
        public void ProcessRunFromStatic()
        {
            string current = Environment.CurrentDirectory;
            RunProcess process = RunProcess.RunFrom("cmd", current, "/c", "dir");
            Assert.That(process.ExitCode, Is.EqualTo(0));
            Assert.That(process.StdOut, Is.Not.Empty);

            // Dump the contents for evaluation later
            foreach (string line in process.StdOut) {
                Console.WriteLine("stdout: {0}", line);
            }
            foreach (string line in process.StdErr) {
                Console.WriteLine("stderr: {0}", line);
            }
        }

        [Test]
        [Platform(Include = "Win32")]
        public void ProcessRunFromAsyncResult()
        {
            string current = Environment.CurrentDirectory;
            RunProcess process = new RunProcess("cmd", current, new[] { "/c", "dir" });
            ProcessRunFromAsyncResult(process);
        }

        [Test]
        public void ProcessSimRunFromAsyncResult()
        {
            string current = Environment.CurrentDirectory;
            RunProcess process = new GetDirSimProcess("cmd", current, "/c dir");
            ProcessRunFromAsyncResult(process);
        }

        private static void ProcessRunFromAsyncResult(RunProcess process)
        {
            IAsyncResult ar = process.BeginExecute(null, null);
            process.EndExecute(ar);

            Assert.That(process.ExitCode, Is.EqualTo(0));
            Assert.That(process.StdOut, Is.Not.Empty);

            // Dump the contents for evaluation later
            foreach (string line in process.StdOut) {
                Console.WriteLine("stdout: {0}", line);
            }
            foreach (string line in process.StdErr) {
                Console.WriteLine("stderr: {0}", line);
            }
        }

        [Test]
        [Platform(Include = "Win32")]
        public void ProcessRunFromAsyncResultTerminate()
        {
            string current = Environment.CurrentDirectory;
            RunProcess process = new RunProcess("timeout.exe", current, new[] { "10" });
            ProcessRunFromAsyncResultTerminate(process);
        }

        [Test]
        public void ProcessSimRunFromAsyncResultTerminate()
        {
            string current = Environment.CurrentDirectory;
            RunProcess process = new TimeoutSimProcess("timeout.exe", current, "10");
            ProcessRunFromAsyncResultTerminate(process);
        }

        private static void ProcessRunFromAsyncResultTerminate(RunProcess process)
        {
            IAsyncResult ar = process.BeginExecute(null, null);
            Thread.Sleep(100);
            process.Terminate();

            // Note that here, we might not get the exit code, as the process may not have immediately exited
            try {
                int exitCode = process.ExitCode;
                Assert.That(exitCode, Is.EqualTo(-1));
            } catch (InvalidOperationException) { /* Ignore exception */ }

            process.EndExecute(ar);
            Assert.That(process.ExitCode, Is.EqualTo(-1));
        }

        [Test]
        [Platform(Include = "Win32")]
        public void ProcessRunFromAsyncResultTerminateBefore()
        {
            string current = Environment.CurrentDirectory;
            RunProcess process = new RunProcess("timeout.exe", current, new[] { "1" });
            ProcessRunFromAsyncResultTerminateBefore(process);
        }

        [Test]
        public void ProcessSimRunFromAsyncResultTerminateBefore()
        {
            string current = Environment.CurrentDirectory;
            RunProcess process = new TimeoutSimProcess("timeout.exe", current, "1");
            ProcessRunFromAsyncResultTerminateBefore(process);
        }

        private static void ProcessRunFromAsyncResultTerminateBefore(RunProcess process)
        {
            process.Terminate();
            IAsyncResult ar = process.BeginExecute(null, null);

            // Note that here, we might not get the exit code, as the process may not have immediately exited
            try {
                int exitCode = process.ExitCode;
                Assert.That(exitCode, Is.EqualTo(-1));
            } catch (InvalidOperationException) { /* Ignore exception */ }

            process.EndExecute(ar);
            Assert.That(process.ExitCode, Is.EqualTo(-1));
        }

        [Test]
        [Platform(Include = "Win32")]
        public void ProcessRunFromAsyncResultTerminateAfter()
        {
            string current = Environment.CurrentDirectory;
            RunProcess process = new RunProcess("timeout.exe", current, new[] { "1" });
            ProcessRunFromAsyncResultTerminateAfter(process);
        }

        [Test]
        public void ProcessSimRunFromAsyncResultTerminateAfter()
        {
            string current = Environment.CurrentDirectory;
            RunProcess process = new TimeoutSimProcess("timeout.exe", current, "1");
            ProcessRunFromAsyncResultTerminateAfter(process);
        }

        private static void ProcessRunFromAsyncResultTerminateAfter(RunProcess process)
        {
            IAsyncResult ar = process.BeginExecute(null, null);
            Assert.That(() => { _ = process.ExitCode; }, Throws.TypeOf<InvalidOperationException>());
            Thread.Sleep(1200);
            process.Terminate();

            process.EndExecute(ar);
            Assert.That(process.ExitCode, Is.EqualTo(0));
        }

        [Test]
        [Platform(Include = "Win32")]
        public void ProcessRunFromAsyncResultTerminateTwice()
        {
            string current = Environment.CurrentDirectory;
            RunProcess process = new RunProcess("timeout.exe", current, new[] { "10" });
            ProcessRunFromAsyncResultTerminateTwice(process);
        }

        [Test]
        public void ProcessSimRunFromAsyncResultTerminateTwice()
        {
            string current = Environment.CurrentDirectory;
            RunProcess process = new TimeoutSimProcess("timeout.exe", current, "10");
            ProcessRunFromAsyncResultTerminateTwice(process);
        }

        private static void ProcessRunFromAsyncResultTerminateTwice(RunProcess process)
        {
            IAsyncResult ar = process.BeginExecute(null, null);
            Thread.Sleep(100);
            process.Terminate();
            process.Terminate();
            process.EndExecute(ar);
            Assert.That(process.ExitCode, Is.EqualTo(-1));
        }

        [Test]
        [Platform(Include = "Win32")]
        public void ProcessRunFromGetStdOut()
        {
            string current = Environment.CurrentDirectory;
            RunProcess process = new RunProcess("cmd", current, new[] { "/c", "dir" });
            ProcessRunFromGetStdOut(process);
        }

        [Test]
        public void ProcessSimRunFromGetStdOut()
        {
            string current = Environment.CurrentDirectory;
            RunProcess process = new GetDirSimProcess("cmd", current, "/c dir");
            ProcessRunFromGetStdOut(process);
        }

        private static void ProcessRunFromGetStdOut(RunProcess process)
        {
            int lines = 0;
            process.OutputDataReceived += (s, e) => { lines++; };

            IAsyncResult ar = process.BeginExecute(null, null);
            process.EndExecute(ar);

            Assert.That(process.ExitCode, Is.EqualTo(0));
            Assert.That(process.StdOut, Is.Not.Empty);

            // Dump the contents for evaluation later
            foreach (string line in process.StdOut) {
                Console.WriteLine("stdout: {0}", line);
            }
            foreach (string line in process.StdErr) {
                Console.WriteLine("stderr: {0}", line);
            }

            Assert.That(lines, Is.EqualTo(process.StdOut.Count));
        }

        [Test]
        [Platform(Include = "Win32")]
        public async Task ProcessRunFromAsync()
        {
            string current = Environment.CurrentDirectory;
            RunProcess process = new RunProcess("cmd", current, new[] { "/c", "dir" });
            await ProcessRunFromAsync(process);
        }

        [Test]
        public async Task ProcessSimRunFromAsync()
        {
            string current = Environment.CurrentDirectory;
            RunProcess process = new GetDirSimProcess("cmd", current, "/c dir");
            await ProcessRunFromAsync(process);
        }

        private static async Task ProcessRunFromAsync(RunProcess process)
        {
            await process.ExecuteAsync();
            Assert.That(process.ExitCode, Is.EqualTo(0));
            Assert.That(process.StdOut, Is.Not.Empty);

            // Dump the contents for evaluation later
            foreach (string line in process.StdOut) {
                Console.WriteLine("stdout: {0}", line);
            }
            foreach (string line in process.StdErr) {
                Console.WriteLine("stderr: {0}", line);
            }
        }

        [Test]
        [Platform(Include = "Win32")]
        [Timeout(2000)]
        public void ProcessRunFromAsyncCancel()
        {
            string current = Environment.CurrentDirectory;
            RunProcess process = new RunProcess("timeout.exe", current, new[] { "10" });
            ProcessRunFromAsyncCancel(process);
        }

        [Test]
        [Timeout(2000)]
        public void ProcesssIMRunFromAsyncCancel()
        {
            string current = Environment.CurrentDirectory;
            RunProcess process = new TimeoutSimProcess("timeout.exe", current, "10");
            ProcessRunFromAsyncCancel(process);
        }

        private static void ProcessRunFromAsyncCancel(RunProcess process)
        {
            using (CancellationTokenSource tokens = new CancellationTokenSource()) {
                Task t = Task.Delay(100).ContinueWith((_) => { tokens.Cancel(); });
                Assert.That(() => {
                    Task.WaitAll(process.ExecuteAsync(tokens.Token), t);
                }, Throws.TypeOf<AggregateException>().With.InnerException.TypeOf<TaskCanceledException>());
                Assert.That(process.ExitCode, Is.EqualTo(-1));
            }
        }

        [Test]
        [Platform(Include = "Win32")]
        [Timeout(2000)]
        public async Task ProcessRunFromAsyncCancelTerminateBefore()
        {
            string current = Environment.CurrentDirectory;
            RunProcess process = new RunProcess("timeout.exe", current, new[] { "10" });
            await ProcessRunFromAsyncCancelTerminateBefore(process);
        }

        [Test]
        [Timeout(2000)]
        public async Task ProcessSimRunFromAsyncCancelTerminateBefore()
        {
            string current = Environment.CurrentDirectory;
            RunProcess process = new TimeoutSimProcess("timeout.exe", current, "10");
            await ProcessRunFromAsyncCancelTerminateBefore(process);
        }

        private static async Task ProcessRunFromAsyncCancelTerminateBefore(RunProcess process)
        {
            using (CancellationTokenSource tokens = new CancellationTokenSource()) {
                process.Terminate();
                await process.ExecuteAsync(tokens.Token);
                Assert.That(process.ExitCode, Is.EqualTo(-1));
            }
        }

        [Test]
        [Platform(Include = "Win32")]
        [Timeout(2000)]
        [Repeat(10)]
        public void ProcessRunFromAsyncCancelImmediatelyBefore()
        {
            string current = Environment.CurrentDirectory;
            RunProcess process = new RunProcess("timeout.exe", current, new[] { "10" });
            ProcessRunFromAsyncCancelImmediatelyBefore(process);
        }

        [Test]
        [Timeout(2000)]
        [Repeat(10)]
        public void ProcessSimRunFromAsyncCancelImmediatelyBefore()
        {
            string current = Environment.CurrentDirectory;
            RunProcess process = new TimeoutSimProcess("timeout.exe", current, "10");
            ProcessRunFromAsyncCancelImmediatelyBefore(process);
        }

        private static void ProcessRunFromAsyncCancelImmediatelyBefore(RunProcess process)
        {
            using (CancellationTokenSource tokens = new CancellationTokenSource()) {
                tokens.Cancel();
                Assert.That(() => {
                    process.ExecuteAsync(tokens.Token).Wait();
                }, Throws.TypeOf<AggregateException>().With.InnerException.TypeOf<TaskCanceledException>());
                Assert.That(process.ExitCode, Is.EqualTo(-1));
            }
        }

        [Test]
        [Platform(Include = "Win32")]
        [Timeout(2000)]
        [Repeat(10)]
        public void ProcessRunFromAsyncCancelImmediatelyAfter()
        {
            string current = Environment.CurrentDirectory;
            RunProcess process = new RunProcess("timeout.exe", current, new[] { "10" });
            ProcessRunFromAsyncCancelImmediatelyAfter(process);
        }

        [Test]
        [Timeout(2000)]
        [Repeat(10)]
        public void ProcessSimRunFromAsyncCancelImmediatelyAfter()
        {
            string current = Environment.CurrentDirectory;
            RunProcess process = new TimeoutSimProcess("timeout.exe", current, "10");
            ProcessRunFromAsyncCancelImmediatelyAfter(process);
        }

        private static void ProcessRunFromAsyncCancelImmediatelyAfter(RunProcess process)
        {
            using (CancellationTokenSource tokens = new CancellationTokenSource()) {
                Task t = process.ExecuteAsync(tokens.Token);
                tokens.Cancel();
                Assert.That(() => {
                    t.Wait();
                }, Throws.TypeOf<AggregateException>().With.InnerException.TypeOf<TaskCanceledException>());
                Assert.That(process.ExitCode, Is.EqualTo(-1));
            }
        }

        [Test]
        [Platform(Include = "Win32")]
        [Timeout(2000)]
        [Repeat(10)]
        public void ProcessRunFromAsyncCancelImmediatelyParallel()
        {
            string current = Environment.CurrentDirectory;
            RunProcess process = new RunProcess("timeout.exe", current, new[] { "10" });
            ProcessRunFromAsyncCancelImmediatelyParallel(process);
        }

        [Test]
        [Timeout(2000)]
        [Repeat(10)]
        public void ProcessSimRunFromAsyncCancelImmediatelyParallel()
        {
            string current = Environment.CurrentDirectory;
            RunProcess process = new TimeoutSimProcess("timeout.exe", current, "10");
            ProcessRunFromAsyncCancelImmediatelyParallel(process);
        }

        private static void ProcessRunFromAsyncCancelImmediatelyParallel(RunProcess process)
        {
            using (CancellationTokenSource tokens = new CancellationTokenSource()) {
                Task c = Task.Run(() => { tokens.Cancel(); });
                Task p = process.ExecuteAsync(tokens.Token);
                Assert.That(() => {
                    Task.WaitAll(c, p);
                }, Throws.TypeOf<AggregateException>().With.InnerException.TypeOf<TaskCanceledException>());
                Assert.That(process.ExitCode, Is.EqualTo(-1));
            }
        }

        [Test]
        [Platform(Include = "Win32")]
        [Timeout(2000)]
        public void ProcessRunFromAsyncTerminate()
        {
            string current = Environment.CurrentDirectory;
            RunProcess process = new RunProcess("timeout.exe", current, new[] { "10" });
            ProcessRunFromAsyncTerminate(process);
        }

        [Test]
        [Timeout(2000)]
        public void ProcessSimRunFromAsyncTerminate()
        {
            string current = Environment.CurrentDirectory;
            RunProcess process = new TimeoutSimProcess("timeout.exe", current, "10");
            ProcessRunFromAsyncTerminate(process);
        }

        private static void ProcessRunFromAsyncTerminate(RunProcess process)
        {
            Task t = Task.Delay(100).ContinueWith((_) => { process.Terminate(); });
            Task.WaitAll(process.ExecuteAsync(), t);
            Assert.That(process.ExitCode, Is.EqualTo(-1));
        }

        [Test]
        [Platform(Include = "Win32")]
        [Timeout(2000)]
        public async Task ProcessRunFromAsyncTerminateBefore()
        {
            string current = Environment.CurrentDirectory;
            RunProcess process = new RunProcess("timeout.exe", current, new[] { "10" });
            await ProcessRunFromAsyncTerminateBefore(process);
        }

        [Test]
        [Timeout(2000)]
        public async Task ProcessRimRunFromAsyncTerminateBefore()
        {
            string current = Environment.CurrentDirectory;
            RunProcess process = new TimeoutSimProcess("timeout.exe", current, "10");
            await ProcessRunFromAsyncTerminateBefore(process);
        }

        private static async Task ProcessRunFromAsyncTerminateBefore(RunProcess process)
        {
            process.Terminate();
            await process.ExecuteAsync();
            Assert.That(process.ExitCode, Is.EqualTo(-1));
        }

        [Test]
        [Platform(Include = "Win32")]
        public async Task ProcessRunFromAsyncStatic()
        {
            string current = Environment.CurrentDirectory;

            RunProcess process = await RunProcess.RunFromAsync("cmd", current, "/c", "dir");
            Assert.That(process.ExitCode, Is.EqualTo(0));
            Assert.That(process.StdOut, Is.Not.Empty);

            // Dump the contents for evaluation later
            foreach (string line in process.StdOut) {
                Console.WriteLine("stdout: {0}", line);
            }
            foreach (string line in process.StdErr) {
                Console.WriteLine("stderr: {0}", line);
            }
        }

        [Test]
        [Platform(Include = "Win32")]
        [Timeout(2000)]
        public void ProcessRunFromAsyncStaticCancel()
        {
            string current = Environment.CurrentDirectory;

            using (CancellationTokenSource tokens = new CancellationTokenSource()) {
                Task<RunProcess> process = RunProcess.RunFromAsync("timeout.exe", current, new[] { "10" }, tokens.Token);
                Task t = Task.Delay(100).ContinueWith((_) => { tokens.Cancel(); });

                Assert.That(() => {
                    Task.WaitAll(process, t);
                }, Throws.TypeOf<AggregateException>().With.InnerException.TypeOf<TaskCanceledException>());

                Assert.That(() => {
                    // It's not possible to get the exit code here.
                    _ = process.Result.ExitCode;
                }, Throws.TypeOf<AggregateException>().With.InnerException.TypeOf<TaskCanceledException>());
            }
        }

        [Test]
        [Platform(Include = "Win32")]
        public async Task ProcessRunAsyncStatic()
        {
            RunProcess process = await RunProcess.RunAsync("cmd", "/c", "dir");
            Assert.That(process.ExitCode, Is.EqualTo(0));
            Assert.That(process.StdOut, Is.Not.Empty);

            // Dump the contents for evaluation later
            foreach (string line in process.StdOut) {
                Console.WriteLine("stdout: {0}", line);
            }
            foreach (string line in process.StdErr) {
                Console.WriteLine("stderr: {0}", line);
            }
        }

        [Test]
        [Platform(Include = "Win32")]
        [Timeout(2000)]
        public void ProcessRunAsyncStaticCancel()
        {
            using (CancellationTokenSource tokens = new CancellationTokenSource()) {
                Task<RunProcess> process = RunProcess.RunAsync("timeout.exe", new[] { "10" }, tokens.Token);
                Task t = Task.Delay(100).ContinueWith((_) => { tokens.Cancel(); });

                Assert.That(() => {
                    Task.WaitAll(process, t);
                }, Throws.TypeOf<AggregateException>().With.InnerException.TypeOf<TaskCanceledException>());

                Assert.That(() => {
                    // It's not possible to get the exit code here.
                    _ = process.Result.ExitCode;
                }, Throws.TypeOf<AggregateException>().With.InnerException.TypeOf<TaskCanceledException>());
            }
        }

        [Test]
        [Platform(Include = "Win32")]
        public void ProcessExecuteTwice()
        {
            string currentDir = Environment.CurrentDirectory;
            RunProcess process = new RunProcess("cmd", currentDir, new string[] { "/c", "dir" });
            ProcessExecuteTwice(process);
        }

        [Test]
        public void ProcessSimExecuteTwice()
        {
            string currentDir = Environment.CurrentDirectory;
            RunProcess process = new GetDirSimProcess("cmd", currentDir, "/c dir");
            ProcessExecuteTwice(process);
        }

        private static void ProcessExecuteTwice(RunProcess process)
        {
            process.Execute();
            Assert.That(() => {
                process.Execute();
            }, Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        [Platform(Include = "Win32")]
        public async Task ProcessExecuteAsyncTwice()
        {
            string currentDir = Environment.CurrentDirectory;
            RunProcess process = new RunProcess("cmd", currentDir, new string[] { "/c", "dir" });
            await ProcessExecuteAsyncTwice(process);
        }

        [Test]
        public async Task ProcessSimExecuteAsyncTwice()
        {
            string currentDir = Environment.CurrentDirectory;
            RunProcess process = new GetDirSimProcess("cmd", currentDir, "/c dir");
            await ProcessExecuteAsyncTwice(process);
        }

        private static async Task ProcessExecuteAsyncTwice(RunProcess process)
        {
            await process.ExecuteAsync();
            Assert.That(async () => {
                await process.ExecuteAsync();
            }, Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        [Platform(Include = "Win32")]
        public async Task ProcessExecuteAsyncWithTokenTwice()
        {
            string currentDir = Environment.CurrentDirectory;
            RunProcess process = new RunProcess("cmd", currentDir, new string[] { "/c", "dir" });
            await ProcessExecuteAsyncWithTokenTwice(process);
        }

        [Test]
        public async Task ProcessSimExecuteAsyncWithTokenTwice()
        {
            string currentDir = Environment.CurrentDirectory;
            RunProcess process = new GetDirSimProcess("cmd", currentDir, "/c dir");
            await ProcessExecuteAsyncWithTokenTwice(process);
        }

        private static async Task ProcessExecuteAsyncWithTokenTwice(RunProcess process)
        {
            using (CancellationTokenSource tokens = new CancellationTokenSource()) {
                await process.ExecuteAsync(tokens.Token);
                Assert.That(async () => {
                    await process.ExecuteAsync(tokens.Token);
                }, Throws.TypeOf<InvalidOperationException>());
            }
        }

        [Test]
        [Platform(Include = "Win32")]
        public void ProcessBeginExecuteTwice()
        {
            string currentDir = Environment.CurrentDirectory;
            RunProcess process = new RunProcess("cmd", currentDir, new string[] { "/c", "dir" });
            ProcessBeginExecuteTwice(process);
        }

        [Test]
        public void ProcessSimBeginExecuteTwice()
        {
            string currentDir = Environment.CurrentDirectory;
            RunProcess process = new GetDirSimProcess("cmd", currentDir, "/c dir");
            ProcessBeginExecuteTwice(process);
        }

        private static void ProcessBeginExecuteTwice(RunProcess process)
        {
            var ar = process.BeginExecute(null, null);
            process.EndExecute(ar);

            Assert.That(() => {
                _ = process.BeginExecute(null, null);
            }, Throws.TypeOf<InvalidOperationException>());
        }
    }
}
