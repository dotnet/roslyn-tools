using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace roslyn.optprof.lib
{
    public static class Manifest
    {
        public const string IBC = "IBC";
        public const string ARGS = "/ExeConfig:\"%VisualStudio.InstallationUnderTest.Path%\\Common7\\IDE\\vsn.exe\"";
        public const string ROOT = "%VisualStudio.InstallationUnderTest.Path%";

        public static IEnumerable<(string Technology, string RelativeInstallationPath, string InstrumentationArguments)> GetNgenEntriesFromJsonManifest(JObject json)
        {
            if (json["extensionDir"] != null)
            {
                var extensionDir = ((string)json["extensionDir"]).Replace("[installdir]\\", string.Empty);
                return ((JArray)json["files"])
                    .Where(file => IsNgened(file) && IsAssembly(file))
                    .Select(file =>
                    {
                        string Technology = IBC;
                        string RelativeInstallationPath = $"{extensionDir}\\{((string)file["fileName"]).Replace("/", string.Empty)}";
                        string InstrumentationArguments = ARGS;
                        return (Technology, RelativeInstallationPath, InstrumentationArguments);
                    });
            }
            else
            {
                return ((JArray)json["files"])
                    .Where(file => IsNgened(file) && IsAssembly(file))
                    .Select(file =>
                    {
                        string Technology = IBC;
                        string RelativeInstallationPath = ((string)file["fileName"]).Replace("/Contents/", string.Empty).Replace("/", "\\");
                        string InstrumentationArguments = file["ngenApplication"] != null
                            ? $"/ExeConfig:\"{ROOT}{((string)file["ngenApplication"]).Replace("[installDir]", string.Empty)}\""
                            : ARGS;
                        return (Technology, RelativeInstallationPath, InstrumentationArguments);
                    });
            }
        }

        private static bool IsNgened(JToken file)
            => file["ngen"] != null || file["ngenPriority"] != null || file["ngenArchitecture"] != null || file["ngenApplication"] != null;


        private static bool IsAssembly(JToken file)
        {
            if (file["fileName"] == null)
            {
                return false;
            }

            var fileName = (string)file["fileName"];
            return fileName.EndsWith("dll") || fileName.EndsWith("exe");
        }
    }
}
