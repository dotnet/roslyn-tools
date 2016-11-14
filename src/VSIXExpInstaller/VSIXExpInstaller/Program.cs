using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.ExtensionManager;
using Microsoft.VisualStudio.Settings;

namespace VsixExpInstaller
{
    public class Program
    {
        static string ExtractArg(List<string> args, string prefix)
        {
            for (int i = 0; i < args.Count; i++)
            {
                if (args[i].StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    string result = args[i].Substring(prefix.Length);
                    args.RemoveAt(i);
                    return result;
                }
            }

            return null;
        }

        static bool FindArg(List<string> args, string argName)
        {
            for (int i = 0; i < args.Count; i++)
            {
                if (String.Compare(args[i], argName, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    args.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        static void PrintUsage()
        {
            Console.WriteLine("Usage: VSIXExpInstaller [/rootSuffix:suffix] [/u] [/uninstallAll] [/vsInstallDir:vsInstallDir] <vsix>");
            Console.WriteLine("Suffix is Exp by default.");
        }

        static void UninstallAll(ExtensionManagerService service)
        {
            // We only want extensions which are not installed per machine and we want them sorted by their dependencies.
            var installedExtensions = service.GetInstalledExtensions()
                                             .Where((installedExtension) => !installedExtension.InstalledPerMachine)
                                             .OrderBy((installedExtension) => installedExtension,
                                                      Comparer<IInstalledExtension>.Create((left, right) =>
                                                      {
                                                          if (left.References.Count() == 0)
                                                          {
                                                              // When left.References.Count() is zero, then we have two scenarios:
                                                              //    * right.References.Count() is zero, and the order of the two components doesn't matter, so we return 0
                                                              //    * right.References.Count() is not zero, which means it should be uninstalled after left, so we return -1
                                                              return right.References.Count() == 0 ? 0 : -1;
                                                          }
                                                          else if (right.References.Count() == 0)
                                                          {
                                                              // When left.References.Count() is not zero, but right.References.Count() is, then we have one scenario:
                                                              //    * right should be uninstalled before left, so we return 1
                                                              return 1;
                                                          }

                                                          if (left.References.Any((extensionReference) => extensionReference.Identifier == right.Header.Identifier))
                                                          {
                                                              // When left.References contains right, then we have one scenario:
                                                              //    * left is dependent on right, which means it must be uninstalled afterwards, so we return 1
                                                              return 1;
                                                          }
                                                          else if (right.References.Any((extensionReference) => extensionReference.Identifier == left.Header.Identifier))
                                                          {
                                                              // When right.References contains left, then we have one scenario:
                                                              //    * right is dependent on left, which means it must be uninstalled afterwards, so we return -1
                                                              return -1;
                                                          }

                                                          // Finally, if both projects contain references, but neither depends on the other, we have one scenario:
                                                          //    * left and right are independent of each other, and the order of the two components doesn't matter, so we return 0
                                                          return 0;
                                                      }));

            foreach (var installedExtension in installedExtensions)
            {
                Console.WriteLine("  Uninstalling {0}... ", installedExtension.Header.Name);
                service.Uninstall(installedExtension);
            }
        }

        private static string GetDevenvPath(string vsInstallDir)
        {
            if (string.IsNullOrWhiteSpace(vsInstallDir))
            {
                throw new InvalidOperationException("VSIXExpInstaller needs to be run from the Developer Command Prompt or have VsInstallDir passed in as an argument.");
            }

            return Path.Combine(vsInstallDir, @"Common7\IDE\DevEnv.exe");
        }

        public static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                PrintUsage();
                return 0;
            }

            List<string> argList = new List<string>(args);

            string rootSuffix = ExtractArg(argList, "/rootSuffix:") ?? "Exp";
            bool uninstall = FindArg(argList, "/u");
            bool uninstallAll = FindArg(argList, "/uninstallAll");
            string vsInstallDir = ExtractArg(argList, "/vsInstallDir:") ?? Environment.GetEnvironmentVariable("VsInstallDir");

            var expectedArgCount = uninstallAll ? 0 : 1;

            if (argList.Count != expectedArgCount)
            {
                PrintUsage();
                return 1;
            }

            string vsixPath = uninstallAll ? string.Empty : argList[0];
            string devenvPath;

            try
            {
                devenvPath = GetDevenvPath(vsInstallDir);

                using (ExternalSettingsManager settingsManager = ExternalSettingsManager.CreateForApplication(devenvPath, rootSuffix))
                {
                    ExtensionManagerService extensionManagerService = null;

                    try
                    {
                        extensionManagerService = new ExtensionManagerService(settingsManager);

                        if (uninstallAll)
                        {
                            Console.WriteLine("Uninstalling all... ");
                            UninstallAll(extensionManagerService);
                        }
                        else
                        {
                            var extensionManager = (IVsExtensionManager)(extensionManagerService);
                            IInstallableExtension vsixToInstall = extensionManager.CreateInstallableExtension(vsixPath);

                            bool found = extensionManagerService.TryGetInstalledExtension(vsixToInstall.Header.Identifier, out IInstalledExtension existing) && !existing.InstallPath.StartsWith(vsInstallDir, StringComparison.OrdinalIgnoreCase);
                            if (uninstall)
                            {
                                if (found)
                                {
                                    Console.WriteLine("Uninstalling {0}... ", vsixPath);
                                    extensionManagerService.Uninstall(existing);
                                }
                                else
                                {
                                    Console.WriteLine("Nothing to uninstall... ");
                                }
                            }
                            else
                            {
                                if (found)
                                {
                                    Console.WriteLine("Updating {0}... ", vsixPath);
                                    extensionManagerService.Uninstall(existing);
                                }
                                else
                                {
                                    Console.WriteLine("Installing {0}... ", vsixPath);
                                }

                                extensionManagerService.Install(vsixToInstall, perMachine: false);
                            }
                        }

                        Console.WriteLine("Done!");
                    }
                    finally
                    {
                        extensionManagerService?.Close();
                    }
                }
            }
            catch (Exception e)
            {
                string message = e.GetType().Name + ": " + e.Message + Environment.NewLine + e.ToString();
                Console.Error.WriteLine(message);
                return 2;
            }

            return 0;
        }
    }
}
