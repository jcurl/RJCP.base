namespace RJCP.Sandcastle.Plugin
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Xml;
    using System.Xml.Linq;
    using SandcastleBuilder.Utils.BuildComponent;
    using SandcastleBuilder.Utils.BuildEngine;
    using Topics;

    // Namespace already uses "sandcastle"
    using HelpFileFormats = global::Sandcastle.Core.HelpFileFormats;

    /// <summary>
    /// Plug-in to rename topics to avoid overlap with System topics.
    /// </summary>
    [HelpFileBuilderPlugInExport("RJCP MSHC Namespace Rewriter",
        Version = AssemblyInfo.ProductVersion,
        Copyright = AssemblyInfo.Copyright,
        Description =
            "Rewrites specific topic identifiers in locally generated code " +
            "to not conflict with topics provided by other Microsoft topics."
        )]
    public sealed class HelpIdPlugIn : IPlugIn
    {
        private readonly List<ExecutionPoint> m_ExecutionPoints = new() {
            new ExecutionPoint(BuildStep.CombiningIntermediateTocFiles, ExecutionBehaviors.After),
            new ExecutionPoint(BuildStep.BuildTopics, ExecutionBehaviors.After)
        };

        private BuildProcess m_Builder;

        // The mappings are hard-coded. For future versions, we could consider loading in the mappings from a
        // configuration file.
        private readonly Dictionary<string, string> m_TopicMap = new() {
            { "N:System", "N:_RJCP.System" }
        };

        /// <summary>
        /// This read-only property returns a collection of execution points that define when the plug-in should
        /// be invoked during the build process.
        /// </summary>
        public IEnumerable<ExecutionPoint> ExecutionPoints { get { return m_ExecutionPoints; } }

        /// <summary>
        /// This method is used to initialize the plug-in at the start of the build process.
        /// </summary>
        /// <param name="buildProcess">A reference to the current build process.</param>
        /// <param name="configuration">The configuration data that the plug-in should use to initialize itself.</param>
        public void Initialize(BuildProcess buildProcess, XElement configuration)
        {
            if (buildProcess.CurrentProject.HelpFileFormat.HasFlag(HelpFileFormats.MSHelpViewer)) {
                m_Builder = buildProcess;
            }
            if (m_Builder is null) return;

            var metadata = (HelpFileBuilderPlugInExportAttribute)this.GetType().GetCustomAttributes(
                typeof(HelpFileBuilderPlugInExportAttribute), false)[0];
            m_Builder.ReportProgress("{0} Version {1}\r\n{2}", metadata.Id, metadata.Version, metadata.Copyright);
        }

        /// <summary>
        /// This method is used to execute the plug-in during the build process.
        /// </summary>
        /// <param name="context">The current execution context.</param>
        public void Execute(ExecutionContext context)
        {
            if (m_Builder is null) return;

            switch (context.BuildStep) {
            case BuildStep.CombiningIntermediateTocFiles:
                ParseToc();
                break;
            case BuildStep.BuildTopics:
                PatchHelpFiles();
                break;
            default:
                break;
            }
        }

        private readonly TableOfContents m_TocRename = new();

        private void ParseToc()
        {
            m_Builder.ReportProgress("Building TOC for MS HelpViewer topics (RJCP.HelpId)");

            XmlReaderSettings settings = new() {
                DtdProcessing = DtdProcessing.Prohibit // Disables DTD processing
            };

            string tocFileName = Path.Combine(m_Builder.WorkingFolder, "toc.xml");
            XmlDocument toc = new();
            using (XmlReader reader = XmlReader.Create(tocFileName, settings)) {
                toc.Load(reader);
                XmlNode root = toc.SelectSingleNode("topics");
                if (root is null) {
                    m_Builder.ReportWarning("RJCP001",
                        "Topic file missing root node 'topics'");
                    return;
                }

                foreach (string topicId in m_TopicMap.Keys) {
                    XmlNodeList tocEntry = root.SelectNodes($"//topic[@id='{topicId}']");
                    if (tocEntry.Count > 1) {
                        m_Builder.ReportWarning("RJCP001",
                            "Topic {0}: Found {1} instances - ignoring (expected exactly one instance)",
                            topicId, tocEntry.Count);
                    } else if (tocEntry.Count == 1) {
                        XmlNode entry = tocEntry[0];
                        string fileName = entry.Attributes["file"]?.Value;
                        if (fileName is null) {
                            m_Builder.ReportWarning("RJCP001",
                                "Topic {0}: File entry in 'toc.xml' is missing", topicId);
                        } else {
                            m_Builder.ReportProgress("  Found parent topic {0} in {1}", topicId, fileName);
                            ParentTopic parent = m_TocRename.RenameTopic(fileName, topicId, m_TopicMap[topicId]);
                            foreach (XmlNode child in entry.ChildNodes) {
                                string childFileName = child.Attributes["file"]?.Value;
                                string childTopic = child.Attributes["id"]?.Value;
                                if (childFileName is null) {
                                    m_Builder.ReportWarning("RJCP001",
                                        "Topic {0}: File entry in 'toc.xml' having parent {1} is empty",
                                        topicId, childTopic ?? "(empty)");
                                }
                                if (childTopic is null) {
                                    m_Builder.ReportWarning("RJCP001",
                                        "Topic {0}: Id entry in 'toc.xml' having parent {1} is empty",
                                        topicId);
                                }
                                if (childTopic is not null && childFileName is not null) {
                                    parent.UpdateChild(childFileName);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void PatchHelpFiles()
        {
            m_Builder.ReportProgress("Patching topics for MS Help Viewer topics (RJCP.HelpId)");

            foreach (HelpTopicFile topic in m_TocRename) {
                string current, updated;
                if (topic.Topic is not null) {
                    current = topic.Topic.Current;
                    updated = topic.Topic.Updated;
                } else {
                    current = topic.ParentRef.Current;
                    updated = topic.ParentRef.Updated;
                }
                m_Builder.ReportProgress("  Patching file {0} for {1} -> {2}", topic.File, current, updated);

                string fileName = Path.Combine(m_Builder.WorkingFolder, "Output", "MSHelpViewer", "html", $"{topic.File}.htm");
                if (!File.Exists(fileName)) {
                    m_Builder.ReportWarning("RJCP001",
                        "Topic {0}: File {1} doesn't exist.",
                        current, fileName);
                } else {
                    PatchHelpFile(fileName, topic);
                }
            }
        }

        private void PatchHelpFile(string fileName, HelpTopicFile topic)
        {
            XmlReaderSettings settings = new() {
                DtdProcessing = DtdProcessing.Prohibit // Disables DTD processing
            };

            XmlDocument doc = new() {
                PreserveWhitespace = true
            };
            using (XmlReader reader = XmlReader.Create(fileName, settings)) {
                doc.Load(reader);
                string defaultNamespace = doc.DocumentElement.NamespaceURI;
                XmlNamespaceManager nsmgr = new(doc.NameTable);
                nsmgr.AddNamespace("ns", defaultNamespace);

                if (topic.Topic is not null) {
                    XmlNode topicAttribute = doc.SelectSingleNode("/ns:html/ns:head/ns:meta[@name='Microsoft.Help.Id']", nsmgr);
                    if (topicAttribute is null) {
                        m_Builder.ReportWarning("RJCP001",
                            "Topic {0}: File {1} missing meta element with attribute name 'Microsoft.Help.Id'.",
                            topic.Topic.Current, topic.File);
                    } else {
                        XmlAttribute content = topicAttribute.Attributes["content"];
                        if (content is null) {
                            m_Builder.ReportWarning("RJCP001",
                                "Topic {0}: File {1} missing attribute 'content'.",
                                topic.Topic.Current, topic.File);
                        } else {
                            content.Value = topic.Topic.Updated;
                        }
                    }
                }

                if (topic.ParentRef is not null) {
                    XmlNode parentAttribute = doc.SelectSingleNode("/ns:html/ns:head/ns:meta[@name='Microsoft.Help.TocParent']", nsmgr);
                    if (parentAttribute is null) {
                        m_Builder.ReportWarning("RJCP001",
                            "Topic {0}: File {1} missing meta element with attribute 'Microsoft.Help.TocParent'.",
                            topic.ParentRef.Current, topic.File);
                    } else {
                        XmlAttribute content = parentAttribute.Attributes["content"];
                        if (content is null) {
                            m_Builder.ReportWarning("RJCP001",
                                "Topic {0}: File {1} missing attribute 'content'.",
                                topic.Topic.Current, topic.File);
                        } else {
                            content.Value = topic.ParentRef.Updated;
                        }
                    }
                }
            }

            doc.Save(fileName);
        }

        /// <summary>
        /// This implements the Dispose() interface to properly dispose of the plug-in object
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
