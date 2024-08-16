// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Microsoft.RoslynTools.Authentication
{
    /// <summary>
    /// Reads and writes the settings file.
    /// </summary>
    internal class LocalSettings
    {
        public string GitHubToken { get; set; } = "";
        public string DevDivAzureDevOpsToken { get; set; } = "";
        public string DncEngAzureDevOpsToken { get; set; } = "";

        public int SaveSettingsFile(ILogger logger)
        {
            string settings = JsonConvert.SerializeObject(this);
            return EncodedFile.Create(Constants.SettingsFileName, settings, logger);
        }

        public static LocalSettings LoadSettingsFile()
        {
            string settings = EncodedFile.Read(Constants.SettingsFileName);
            return JsonConvert.DeserializeObject<LocalSettings>(settings)!;
        }

        /// <summary>
        /// Retrieve the settings from the user's settings file.
        /// </summary>
        /// <returns>Settings for use in remote commands</returns>
        /// <remarks>The command line takes precedence over the settings file.</remarks>
        public static RoslynToolsSettings GetRoslynToolsSettings(string githubToken, string devdivAzDOToken, string dncengAzDOToken, bool isCI, ILogger logger)
        {
            var roslynToolsSettings = new RoslynToolsSettings();

            try
            {
                var localSettings = LoadSettingsFile();

                roslynToolsSettings.GitHubToken = localSettings.GitHubToken;
                roslynToolsSettings.DevDivAzureDevOpsToken = localSettings.DevDivAzureDevOpsToken;
                roslynToolsSettings.DncEngAzureDevOpsToken = localSettings.DncEngAzureDevOpsToken;
                roslynToolsSettings.IsCI = isCI;
            }
            catch (Exception e)
            {
                logger.LogWarning(e, $"Failed to load the roslyn-tools settings. File may be corrupted or missing. Run `roslyn-tools authenticate` to regenerate.");
            }

            // Override if non-empty on command line
            if (githubToken.Length > 0)
            {
                roslynToolsSettings.GitHubToken = githubToken;
            }

            if (devdivAzDOToken.Length > 0)
            {
                roslynToolsSettings.DevDivAzureDevOpsToken = devdivAzDOToken;
            }

            if (dncengAzDOToken.Length > 0)
            {
                roslynToolsSettings.DncEngAzureDevOpsToken = dncengAzDOToken;
            }

            return roslynToolsSettings;
        }
    }
}
