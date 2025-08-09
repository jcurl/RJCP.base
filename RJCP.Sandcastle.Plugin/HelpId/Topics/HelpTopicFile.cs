namespace RJCP.Sandcastle.Plugin.Topics
{
    using System;
    using System.Diagnostics;

    [DebuggerDisplay("{File}; Topic={Topic}; ParentRef={ParentRef}")]
    internal class HelpTopicFile
    {
        public HelpTopicFile(string fileName)
        {
            if (fileName is null)
                throw new ArgumentNullException(nameof(fileName));
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("Empty file", nameof(fileName));

            File = fileName;
        }

        /// <summary>
        /// Gets the file name entry for the topic.
        /// </summary>
        /// <value>The file name entry for the topic.</value>
        public string File { get; }

        /// <summary>
        /// Gets or sets the topic rename action.
        /// </summary>
        /// <value>The rename action for the topic.</value>
        /// <remarks>
        /// The topic entry specifies the old name of the topic and the new name of the topic. The topic is identified
        /// by the <c><meta name="Microsoft.Help.Id" content="N:System"/></c>.
        /// </remarks>
        public RenameAction Topic { get; set; }

        /// <summary>
        /// Gets or sets the parent entry rename action.
        /// </summary>
        /// <value>The rename action for the parent entry.</value>
        /// <remarks>
        /// The parent entry specifies the old name of the topic and the new name of the topic that is the parent of
        /// this file name entry. The topic is identified by the
        /// <c><meta name="Microsoft.Help.TocParent" content="N:System"/></c>.
        /// </remarks>
        public RenameAction ParentRef { get; set; }
    }
}
