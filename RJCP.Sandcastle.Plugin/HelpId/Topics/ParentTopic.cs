namespace RJCP.Sandcastle.Plugin.Topics
{
    using System;
    using System.Collections.Generic;

    internal class ParentTopic
    {
        private readonly Dictionary<string, HelpTopicFile> m_Files;
        private readonly string m_CurrentTopic;
        private readonly string m_UpdatedTopic;

        internal ParentTopic(Dictionary<string, HelpTopicFile> files, string current, string updated)
        {
            if (files is null)
                throw new ArgumentNullException(nameof(files));
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

            m_Files = files;
            m_CurrentTopic = current;
            m_UpdatedTopic = updated;
        }

        public void UpdateChild(string fileName)
        {
            if (fileName is null)
                throw new ArgumentNullException(nameof(fileName));
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("Empty file", nameof(fileName));

            if (!m_Files.TryGetValue(fileName, out HelpTopicFile helpTopicFile)) {
                helpTopicFile = new(fileName);
                m_Files.Add(fileName, helpTopicFile);
            }

            if (helpTopicFile.ParentRef is not null)
                throw new ArgumentException($"Topic {fileName} already present with parent {helpTopicFile.ParentRef.Current} to {helpTopicFile.ParentRef.Updated}. Can't rename topic {m_CurrentTopic} to {m_UpdatedTopic}");

            helpTopicFile.ParentRef = new(m_CurrentTopic, m_UpdatedTopic);
        }
    }
}
