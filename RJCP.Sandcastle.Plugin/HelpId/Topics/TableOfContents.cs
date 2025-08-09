namespace RJCP.Sandcastle.Plugin.Topics
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;

    [DebuggerDisplay("Entries={m_Files.Count}")]
    internal class TableOfContents : IEnumerable<HelpTopicFile>
    {
        private readonly Dictionary<string, HelpTopicFile> m_Files = new();

        public ParentTopic RenameTopic(string fileName, string current, string updated)
        {
            if (fileName is null)
                throw new ArgumentNullException(nameof(fileName));
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("Empty file", nameof(fileName));
            if (current is null)
                throw new ArgumentNullException(nameof(current));
            if (updated is null)
                throw new ArgumentNullException(nameof(updated));
            if (string.IsNullOrWhiteSpace(current))
                throw new ArgumentException("Empty topic identifier", nameof(current));
            if (string.IsNullOrWhiteSpace(updated))
                throw new ArgumentException("Empty topic identifier", nameof(updated));
            if (string.Compare(current, updated, StringComparison.OrdinalIgnoreCase) == 0)
                throw new ArgumentException("Rename of topic only changes case");

            if (!m_Files.TryGetValue(fileName, out HelpTopicFile helpTopicFile)) {
                helpTopicFile = new(fileName);
                m_Files.Add(fileName, helpTopicFile);
            }

            if (helpTopicFile.Topic is not null)
                throw new ArgumentException($"Topic {fileName} already present with topic {helpTopicFile.Topic.Current} to {helpTopicFile.Topic.Updated}. Can't rename topic {current} to {updated}");

            helpTopicFile.Topic = new(current, updated);
            return new(m_Files, current, updated);
        }

        public IEnumerator<HelpTopicFile> GetEnumerator()
        {
            return m_Files.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
