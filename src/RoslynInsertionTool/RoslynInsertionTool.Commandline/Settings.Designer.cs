// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

namespace Roslyn.Insertion {


    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "15.3.0.0")]
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {

        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));

        public static Settings Default {
            get {
                return defaultInstance;
            }
        }

        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("vslsnap@microsoft.com")]
        public string VisualStudioRepoAzdoUserName {
            get {
                return ((string)(this["VisualStudioRepoAzdoUserName"]));
            }
        }

        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("https://devdiv.visualstudio.com/DefaultCollection")]
        public string VisualStudioRepoAzdoUri {
            get {
                return ((string)(this["VisualStudioRepoAzdoUri"]));
            }
        }

        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("DevDiv")]
        public string VisualStudioRepoProjectName {
            get {
                return ((string)(this["VisualStudioRepoProjectName"]));
            }
        }

        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("\\\\cpvsbuild\\drops\\Roslyn")]
        public string BuildDropPath {
            get {
                return ((string)(this["BuildDropPath"]));
            }
        }

        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("dev/vslsnap/insertions/")]
        public string InsertionBranchName {
            get {
                return ((string)(this["InsertionBranchName"]));
            }
        }

        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("mlinfraswat@microsoft.com")]
        public string MailRecipient {
            get {
                return ((string)(this["MailRecipient"]));
            }
        }

        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("smtphost")]
        public string EmailServerName {
            get {
                return ((string)(this["EmailServerName"]));
            }
        }

        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("Roslyn-Signed")]
        public string BuildQueueName {
            get {
                return ((string)(this["BuildQueueName"]));
            }
        }

        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("Release")]
        public string BuildConfig {
            get {
                return ((string)(this["BuildConfig"]));
            }
        }

        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("C:\\VS")]
        public string EnlistmentPath {
            get {
                return ((string)(this["EnlistmentPath"]));
            }
        }

        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool InsertCoreXTPackages {
            get {
                return ((bool)(this["InsertCoreXTPackages"]));
            }
        }

        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool InsertDevDivSourceFiles {
            get {
                return ((bool)(this["InsertDevDivSourceFiles"]));
            }
        }

        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool InsertWillowPackages {
            get {
                return ((bool)(this["InsertWillowPackages"]));
            }
        }

        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("Roslyn")]
        public string InsertionName {
            get {
                return ((string)(this["InsertionName"]));
            }
        }

        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool RetainInsertedBuild {
            get {
                return ((bool)(this["RetainInsertedBuild"]));
            }
        }

        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("https://managedlanguages.vault.azure.net")]
        public string KeyVaultUrl {
            get {
                return ((string)(this["KeyVaultUrl"]));
            }
        }

        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("vslsnap-vso-auth-token")]
        public string VisualStudioRepoSecretName {
            get {
                return ((string)(this["VisualStudioRepoSecretName"]));
            }
        }

        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("vslsnap-build-auth-token")]
        public string ComponentBuildSecretName
        {
            get
            {
                return ((string)(this["ComponentBuildSecretName"]));
            }
        }

        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("1950a258-227b-4e31-a9cf-717495945fc2")]
        public string ApplicationId {
            get {
                return ((string)(this["ApplicationId"]));
            }
        }

        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool QueueValidationBuild {
            get {
                return ((bool)(this["QueueValidationBuild"]));
            }
        }

        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("DD-VS-VAL-VSALL (dev15 efforts)")]
        public string ValidationBuildQueueName {
            get {
                return ((string)(this["ValidationBuildQueueName"]));
            }
        }

        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool RunDDRITsInValidation {
            get {
                return ((bool)(this["RunDDRITsInValidation"]));
            }
        }

        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool RunRPSInValidation {
            get {
                return ((bool)(this["RunRPSInValidation"]));
            }
        }

        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool UpdateCoreXTLibraries {
            get
            {
                return ((bool)(this["UpdateCoreXTLibraries"]));
            }
        }

        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("rit.log")]
        public string LogFileLocation {
            get {
                return ((string)(this["LogFileLocation"]));
            }
        }

        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool CreateDraftPr
        {
            get
            {
                return ((bool)(this["CreateDraftPr"]));
            }
        }

        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string SkipCoreXTPackages
        {
            get
            {
                return ((string)(this["SkipCoreXTPackages"]));
            }
        }
    }
}
