using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Principal;
using Microsoft.VisualStudio.ExtensionManager;
using Microsoft.VisualStudio.Settings;

namespace VsixExpInstaller
{
    public class Program
    {
        const string ExtensionManagerCollectionPath = "ExtensionManager";

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

        private static void EnableLoadingAllExtensions(WritableSettingsStore settingsStore)
        {
            const string EnableAdminExtensionsProperty = "EnableAdminExtensions";

            if (!settingsStore.CollectionExists(ExtensionManagerCollectionPath))
            {
                settingsStore.CreateCollection(ExtensionManagerCollectionPath);
            }

            if (!settingsStore.GetBoolean(ExtensionManagerCollectionPath, EnableAdminExtensionsProperty, defaultValue: false))
            {
                settingsStore.SetBoolean(ExtensionManagerCollectionPath, EnableAdminExtensionsProperty, value: true);
            }
        }

        private static void RemoveExtensionFromPendingDeletions(WritableSettingsStore settingsStore, IExtensionHeader vsixToInstallHeader)
        {
            const string PendingDeletionsCollectionPath = ExtensionManagerCollectionPath + @"\PendingDeletions";
            var vsixToDeletePropery = $"{vsixToInstallHeader.Identifier},{vsixToInstallHeader.Version}";

            if (settingsStore.CollectionExists(PendingDeletionsCollectionPath) &&
                settingsStore.PropertyExists(PendingDeletionsCollectionPath, vsixToDeletePropery))
            {
                settingsStore.DeleteProperty(PendingDeletionsCollectionPath, vsixToDeletePropery);
            }
        }

        private static void UpdateLastExtensionsChange(WritableSettingsStore settingsStore)
        {
            const string ExtensionsChangedProperty = "ExtensionsChanged";

            if (!settingsStore.CollectionExists(ExtensionManagerCollectionPath))
            {
                settingsStore.CreateCollection(ExtensionManagerCollectionPath);
            }

            settingsStore.SetInt64(ExtensionManagerCollectionPath, ExtensionsChangedProperty, value: DateTime.UtcNow.ToFileTimeUtc());
        }

        private static bool IsRunningAsAdmin()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
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

                using (var settingsManager = ExternalSettingsManager.CreateForApplication(devenvPath, rootSuffix))
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
                            var vsixToInstall = extensionManager.CreateInstallableExtension(vsixPath);
                            var vsixToInstallHeader = vsixToInstall.Header;

                            var foundBefore = extensionManagerService.TryGetInstalledExtension(vsixToInstallHeader.Identifier, out var installedVsixBefore);
                            var installedGloballyBefore = installedVsixBefore.InstallPath.StartsWith(vsInstallDir, StringComparison.OrdinalIgnoreCase);

                            if (uninstall)
                            {
                                if (foundBefore && !installedGloballyBefore)
                                {
                                    Console.WriteLine("Uninstalling {0}... ", vsixPath);
                                    extensionManagerService.Uninstall(installedVsixBefore);
                                }
                                else
                                {
                                    Console.WriteLine("Nothing to uninstall... ");
                                }
                            }
                            else
                            {
                                if (foundBefore && !installedGloballyBefore)
                                {
                                    Console.WriteLine("Updating {0}... ", vsixPath);
                                    extensionManagerService.Uninstall(installedVsixBefore);
                                }
                                else
                                {
                                    Console.WriteLine("Installing {0}... ", vsixPath);
                                }

                                extensionManagerService.Install(vsixToInstall, perMachine: false);
                                var settingsStore = settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);

                                EnableLoadingAllExtensions(settingsStore);
                                RemoveExtensionFromPendingDeletions(settingsStore, vsixToInstallHeader);
                                UpdateLastExtensionsChange(settingsStore);

                                // Recreate the extensionManagerService to force the extension cache to recreate
                                extensionManagerService?.Close();
                                extensionManagerService = new ExtensionManagerService(settingsManager);

                                var foundAfter = extensionManagerService.TryGetInstalledExtension(vsixToInstallHeader.Identifier, out var installedVsixAfter);
                                var installedGloballyAfter = installedVsixAfter.InstallPath.StartsWith(vsInstallDir, StringComparison.OrdinalIgnoreCase);

                                if (uninstall && foundAfter)
                                {
                                    if (installedGloballyBefore && installedGloballyAfter)
                                    {
                                        throw new Exception($"The extension failed to uninstall. It is still installed globally.");
                                    }
                                    else if (!installedGloballyBefore && installedGloballyAfter)
                                    {
                                        Console.WriteLine("The local extension was succesfully uninstalled. However, the global extension is still installed.");
                                    }
                                    else
                                    {
                                        Console.WriteLine("The extension was succesfully uninstalled.");
                                    }
                                }
                                else if (!uninstall)
                                {
                                    if (!foundAfter)
                                    {
                                        throw new Exception($"The extension failed to install. It could not be located.");
                                    }
                                    else if (installedVsixAfter.Header.Version != vsixToInstallHeader.Version)
                                    {
                                        throw new Exception("The extension failed to install. The located version does not match the expected version.");
                                    }
                                    else
                                    {
                                        Console.WriteLine("The extension was succesfully installed.");
                                    }
                                }
                            }
                        }
                    }
                    finally
                    {
                        extensionManagerService?.Close();
                        extensionManagerService = null;
                    }

                    Console.WriteLine("Done!");
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
