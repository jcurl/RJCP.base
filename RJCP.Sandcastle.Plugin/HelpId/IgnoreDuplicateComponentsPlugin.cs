namespace RJCP.Sandcastle.Plugin
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Xml;
    using System.Xml.Linq;
    using SandcastleBuilder.Utils.BuildComponent;
    using SandcastleBuilder.Utils.BuildEngine;

    [HelpFileBuilderPlugInExport("RJCP Ignore Duplicate Components",
        Version = AssemblyInfo.ProductVersion,
        Copyright = AssemblyInfo.Copyright,
        Description =
            "Disables BE0066: CopyFromIndexComponent: Entries for the key 'X' occur"
        )]
    public sealed class IgnoreDuplicateComponentsPlugin : IPlugIn
    {
        private readonly List<ExecutionPoint> m_ExecutionPoints = new() {
            new ExecutionPoint(BuildStep.CreateBuildAssemblerConfigs, ExecutionBehaviors.After)
        };

        private BuildProcess m_Builder;

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
            m_Builder = buildProcess;
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
            case BuildStep.CreateBuildAssemblerConfigs:
                PatchReflection();
                break;
            default:
                break;
            }
        }

        private void PatchReflection()
        {
            m_Builder.ReportProgress("Patching BuildAssembler.config to ignore duplicates (RJCP.IgnoreDuplicateComponents)");

            XmlReaderSettings settings = new() {
                DtdProcessing = DtdProcessing.Prohibit // Disables DTD processing
            };

            string configFileName = Path.Combine(m_Builder.WorkingFolder, "BuildAssembler.config");
            XmlDocument buildCfg = new();
            using (XmlReader reader = XmlReader.Create(configFileName, settings)) {
                buildCfg.Load(reader);

                // component[@id='Copy From Index Component']/index[@name='reflection']/data[@files='reflection.xml']
                XmlNode reflection = buildCfg.SelectSingleNode("//component[@id='Copy From Index Component']/index[@name='reflection']/data[@files='reflection.xml']");
                if (reflection is null) {
                    m_Builder.ReportWarning("RJCP002",
                        "Not patching as BuildAssembler.config data section for reflection.xml not found");
                    return;
                }

                // duplicateWarning
                XmlAttribute duplicateWarning = reflection.Attributes["duplicateWarning"];
                if (duplicateWarning is null) {
                    duplicateWarning = buildCfg.CreateAttribute("duplicateWarning");
                    duplicateWarning.Value = false.ToString();
                    reflection.Attributes.Append(duplicateWarning);
                } else {
                    duplicateWarning.Value = false.ToString();
                }
            }

            buildCfg.Save(configFileName);
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
