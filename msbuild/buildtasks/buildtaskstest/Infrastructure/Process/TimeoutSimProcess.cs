namespace RJCP.MSBuildTasks.Infrastructure.Process
{
    using System;
    using System.Threading;

    internal class TimeoutSimProcess : RunProcess
    {
        private static int TimeoutSim(RunProcess process, string command, string arguments, CancellationToken token)
        {
            int timeout = int.Parse(arguments) * 1000;

            int start = Environment.TickCount;
            int current;
            do {
                if (token.IsCancellationRequested) return 0;
                Thread.Sleep(100);
                current = Environment.TickCount;
            } while (unchecked(current - start) < timeout);

            return 0;
        }

        public TimeoutSimProcess(string command, string workDir, string arguments)
            : base(TimeoutSim, command, workDir, arguments) { }
    }
}
