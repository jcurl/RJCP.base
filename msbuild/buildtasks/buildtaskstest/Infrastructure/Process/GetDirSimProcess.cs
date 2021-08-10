namespace RJCP.MSBuildTasks.Infrastructure.Process
{
    using System.Threading;

    internal class GetDirSimProcess : RunProcess
    {
        private static int GetDirSim(RunProcess process, string command, string arguments, CancellationToken token)
        {
            GetDirSimProcess p = (GetDirSimProcess)process;

            p.LogStdOut(" Volume in drive C has no label.");
            p.LogStdOut(" Volume Serial Number is 3A2G-7Z2W");
            p.LogStdOut(string.Empty);
            p.LogStdOut($" Directory of {p.WorkingDirectory}");
            p.LogStdOut(string.Empty);
            p.LogStdOut("06/08/2021  18:31    <DIR>          .");
            p.LogStdOut("06/08/2021  18:31    <DIR>          ..");
            p.LogStdOut("25/05/2021  17:59            29,048 testhost.dll");
            p.LogStdOut("25/05/2021  18:00           149,360 testhost.exe");
            p.LogStdOut("              49 File(s)      6,918,578 bytes");
            p.LogStdOut("              17 Dir(s)  830,622,584,832 bytes free");
            return 0;
        }

        public GetDirSimProcess(string command, string workDir, string arguments)
            : base(GetDirSim, command, workDir, arguments) { }
    }
}
