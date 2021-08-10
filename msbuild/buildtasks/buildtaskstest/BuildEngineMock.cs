namespace RJCP.MSBuildTasks
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Build.Framework;
    using Moq;

    internal class BuildEngineMock
    {
        private readonly List<string> m_Messages = new List<string>();
        private readonly List<BuildErrorEventArgs> m_Errors = new List<BuildErrorEventArgs>();
        private readonly Mock<IBuildEngine> m_BuildEngineMock = new Mock<IBuildEngine>();

        public BuildEngineMock()
        {
            // Remember all messages logged
            m_BuildEngineMock.Setup(m => m.LogMessageEvent(It.IsAny<BuildMessageEventArgs>()))
                .Callback<BuildMessageEventArgs>(args => {
                    m_Messages.Add(args.Message);
                });
            m_BuildEngineMock.Setup(m => m.LogWarningEvent(It.IsAny<BuildWarningEventArgs>()))
                .Callback<BuildWarningEventArgs>(args => {
                    m_Messages.Add(args.Message);
                });
            m_BuildEngineMock.Setup(m => m.LogErrorEvent(It.IsAny<BuildErrorEventArgs>()))
                .Callback<BuildErrorEventArgs>(args => {
                    m_Errors.Add(args);
                    m_Messages.Add(args.Message);
                });
        }

        public IBuildEngine BuildEngine
        {
            get { return m_BuildEngineMock.Object; }
        }

        public IReadOnlyList<BuildErrorEventArgs> BuildErrorEventArgs
        {
            get { return m_Errors; }
        }

        public void DumpErrorEvents()
        {
            foreach (string message in m_Messages) {
                Console.WriteLine($"{message}");
            }
        }
    }
}
