namespace RJCP.MSBuildTasks.Infrastructure.SourceProvider
{
    using System.IO;
    using Infrastructure.Tools;
    using RJCP.CodeQuality.NUnitExtensions;

    internal static class GitProviderRepo
    {
        internal static ScratchPad GetRepo(string repo, out string path)
        {
            GitToolMock git;
            TestToolFactory factory = TestToolFactory.InitToolFactory();

            ScratchPad scratch = null;
            try {
                scratch = Deploy.ScratchPad();
                Deploy.CreateDirectory(Path.Combine(scratch.RelativePath, repo));

                string toplevel = Path.Combine(scratch.Path, repo);
                path = toplevel;

                factory.ToolCreatedEvent += (s, e) => {
                    git = (GitToolMock)e.Tool;
                    git.VirtualTopLevel = toplevel;
                };
                return scratch;
            } catch {
                if (scratch != null) scratch.Dispose();
                throw;
            }
        }
    }
}
