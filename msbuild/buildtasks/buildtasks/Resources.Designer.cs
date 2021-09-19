﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace RJCP.MSBuildTasks {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "16.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("RJCP.MSBuildTasks.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unknown value provided.
        /// </summary>
        internal static string Arg_UnknownValue {
            get {
                return ResourceManager.GetString("Arg_UnknownValue", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to GIT error.
        /// </summary>
        internal static string Git_Error {
            get {
                return ResourceManager.GetString("Git_Error", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to GIT ISO8601 format unrecognized.
        /// </summary>
        internal static string Git_InvalidIso8601 {
            get {
                return ResourceManager.GetString("Git_InvalidIso8601", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to GIT &apos;log&apos; returned no commits for path &apos;{0}&apos;.
        /// </summary>
        internal static string Git_LogNoCommits {
            get {
                return ResourceManager.GetString("Git_LogNoCommits", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to GIT &apos;log&apos; returned unexpected output for path &apos;{0}&apos;.
        /// </summary>
        internal static string Git_LogUnexpectedOutput {
            get {
                return ResourceManager.GetString("Git_LogUnexpectedOutput", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to GIT tools not available.
        /// </summary>
        internal static string Git_ToolsNotAvailable {
            get {
                return ResourceManager.GetString("Git_ToolsNotAvailable", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to GIT returned unexpected output.
        /// </summary>
        internal static string Git_UnexpectedOutput {
            get {
                return ResourceManager.GetString("Git_UnexpectedOutput", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to End operation type was different than Begin.
        /// </summary>
        internal static string Infra_IO_AsyncResult_EndInvalidOperation {
            get {
                return ResourceManager.GetString("Infra_IO_AsyncResult_EndInvalidOperation", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to End was called multiple times for this operation.
        /// </summary>
        internal static string Infra_IO_AsyncResult_EndMultipleCalls {
            get {
                return ResourceManager.GetString("Infra_IO_AsyncResult_EndMultipleCalls", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to End was called on a different object than Begin.
        /// </summary>
        internal static string Infra_IO_AsyncResult_EndWithInvalidObject {
            get {
                return ResourceManager.GetString("Infra_IO_AsyncResult_EndWithInvalidObject", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Result passed represents an operation not supported by this framework.
        /// </summary>
        internal static string Infra_IO_AsyncResult_UnsupportedResult {
            get {
                return ResourceManager.GetString("Infra_IO_AsyncResult_UnsupportedResult", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Object type not compatible.
        /// </summary>
        internal static string Infra_ObjectTypeNotCompatible {
            get {
                return ResourceManager.GetString("Infra_ObjectTypeNotCompatible", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Invalid operation: Must first locate the executable.
        /// </summary>
        internal static string Infra_Process_ExeNotFound {
            get {
                return ResourceManager.GetString("Infra_Process_ExeNotFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Process not exited.
        /// </summary>
        internal static string Infra_Process_NotComplete {
            get {
                return ResourceManager.GetString("Infra_Process_NotComplete", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Process was already executed. Instantiate a new object.
        /// </summary>
        internal static string Infra_Process_RunProcess_ExecuteTwice {
            get {
                return ResourceManager.GetString("Infra_Process_RunProcess_ExecuteTwice", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Illegal character in metadata field.
        /// </summary>
        internal static string Infra_SemVer_IllegalCharInMetaData {
            get {
                return ResourceManager.GetString("Infra_SemVer_IllegalCharInMetaData", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Illegal character in prerelease field.
        /// </summary>
        internal static string Infra_SemVer_IllegalCharInPreRelease {
            get {
                return ResourceManager.GetString("Infra_SemVer_IllegalCharInPreRelease", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Invalid version.
        /// </summary>
        internal static string Infra_SemVer_InvalidVersion {
            get {
                return ResourceManager.GetString("Infra_SemVer_InvalidVersion", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Invalid version, leading zeroes are not allowed.
        /// </summary>
        internal static string Infra_SemVer_InvalidVersionLeadingZero {
            get {
                return ResourceManager.GetString("Infra_SemVer_InvalidVersionLeadingZero", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {0} field must be a positive integer.
        /// </summary>
        internal static string Infra_SemVer_MustBePositive {
            get {
                return ResourceManager.GetString("Infra_SemVer_MustBePositive", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unknown source provider.
        /// </summary>
        internal static string Infra_Source_UnknownProvider {
            get {
                return ResourceManager.GetString("Infra_Source_UnknownProvider", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Initial count must be zero or greater.
        /// </summary>
        internal static string Infra_Tasks_InitialCountZero {
            get {
                return ResourceManager.GetString("Infra_Tasks_InitialCountZero", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Invalid Tool.
        /// </summary>
        internal static string Infra_Tools_InvalidTool {
            get {
                return ResourceManager.GetString("Infra_Tools_InvalidTool", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Couldn&apos;t determine the source provider..
        /// </summary>
        internal static string RevisionControl_CantInstantiateProvider {
            get {
                return ResourceManager.GetString("RevisionControl_CantInstantiateProvider", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Revision Control Filesystem indicates files have been modified. Please check in. Path is &apos;{0}&apos;..
        /// </summary>
        internal static string RevisionControl_IsDirty {
            get {
                return ResourceManager.GetString("RevisionControl_IsDirty", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to No label provided in project file and strict mode is enabled..
        /// </summary>
        internal static string RevisionControl_LabelNotDefined {
            get {
                return ResourceManager.GetString("RevisionControl_LabelNotDefined", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Label &apos;{0}&apos; not found in Revision Control, or contents have changed. Path is &apos;{1}&apos;..
        /// </summary>
        internal static string RevisionControl_NotLabelled {
            get {
                return ResourceManager.GetString("RevisionControl_NotLabelled", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Path provided doesn&apos;t exist..
        /// </summary>
        internal static string RevisionControl_PathDirectoryDoesntExist {
            get {
                return ResourceManager.GetString("RevisionControl_PathDirectoryDoesntExist", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Path provided is a file, it must be a directory..
        /// </summary>
        internal static string RevisionControl_PathMustBeDirectory {
            get {
                return ResourceManager.GetString("RevisionControl_PathMustBeDirectory", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Path must be provided (it can&apos;t be empty)..
        /// </summary>
        internal static string RevisionControl_PathNotDefined {
            get {
                return ResourceManager.GetString("RevisionControl_PathNotDefined", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The Revision Control Type is not defined..
        /// </summary>
        internal static string RevisionControl_RevisionControlTypeNotDefined {
            get {
                return ResourceManager.GetString("RevisionControl_RevisionControlTypeNotDefined", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The Strict mode isn&apos;t recognized..
        /// </summary>
        internal static string RevisionControl_UnknownStrictMode {
            get {
                return ResourceManager.GetString("RevisionControl_UnknownStrictMode", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The version parameter is empty..
        /// </summary>
        internal static string SemVer_VersionNotProvided {
            get {
                return ResourceManager.GetString("SemVer_VersionNotProvided", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The version &apos;{0}&apos; is not supported..
        /// </summary>
        internal static string SemVer_VersionNotSupported {
            get {
                return ResourceManager.GetString("SemVer_VersionNotSupported", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The signtool.exe could not be found.
        /// </summary>
        internal static string SignTool_ToolsNotAvailable {
            get {
                return ResourceManager.GetString("SignTool_ToolsNotAvailable", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Certificate file not defined.
        /// </summary>
        internal static string X509_Cert_ArgumentNull {
            get {
                return ResourceManager.GetString("X509_Cert_ArgumentNull", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The certificate path property CertPath is not defined..
        /// </summary>
        internal static string X509_Cert_CertPathNotDefined {
            get {
                return ResourceManager.GetString("X509_Cert_CertPathNotDefined", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Certificate file &apos;{0}&apos; couldn&apos;t be opened: {1}.
        /// </summary>
        internal static string X509_Cert_FileInvalid {
            get {
                return ResourceManager.GetString("X509_Cert_FileInvalid", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The certificate path property CertPath &apos;{0}&apos; cannot be found..
        /// </summary>
        internal static string X509_Cert_FileNotFound {
            get {
                return ResourceManager.GetString("X509_Cert_FileNotFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Certificate file &apos;{0}&apos; to be used.
        /// </summary>
        internal static string X509_Cert_Found {
            get {
                return ResourceManager.GetString("X509_Cert_Found", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Error Signing {0} with {1}: {2}..
        /// </summary>
        internal static string X509_Cert_SignError {
            get {
                return ResourceManager.GetString("X509_Cert_SignError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Signing &apos;{0}&apos; with Certificate file &apos;{1}&apos;..
        /// </summary>
        internal static string X509_Cert_SignMessage {
            get {
                return ResourceManager.GetString("X509_Cert_SignMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The artifact to sign InputAssembly is not defined..
        /// </summary>
        internal static string X509_Input_AssemblyNotDefined {
            get {
                return ResourceManager.GetString("X509_Input_AssemblyNotDefined", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The artifact &apos;{0}&apos; cannot be found..
        /// </summary>
        internal static string X509_Input_FileNotFound {
            get {
                return ResourceManager.GetString("X509_Input_FileNotFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to SignTool failed with exit code {0}..
        /// </summary>
        internal static string X509_SignTool_Failure {
            get {
                return ResourceManager.GetString("X509_SignTool_Failure", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to SignTool found at &apos;{0}&apos;..
        /// </summary>
        internal static string X509_SignTool_Found {
            get {
                return ResourceManager.GetString("X509_SignTool_Found", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to SignTool completed with exit code 0..
        /// </summary>
        internal static string X509_SignTool_Success {
            get {
                return ResourceManager.GetString("X509_SignTool_Success", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Using Timestamp: {0}.
        /// </summary>
        internal static string X509_TimeStamp_Found {
            get {
                return ResourceManager.GetString("X509_TimeStamp_Found", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The timestamp URI is empty and won&apos;t be used..
        /// </summary>
        internal static string X509_TimeStamp_Ignored {
            get {
                return ResourceManager.GetString("X509_TimeStamp_Ignored", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The timestamp URI &apos;{0}&apos; is not valid..
        /// </summary>
        internal static string X509_TimeStamp_Invalid {
            get {
                return ResourceManager.GetString("X509_TimeStamp_Invalid", resourceCulture);
            }
        }
    }
}
