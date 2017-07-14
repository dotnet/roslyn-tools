// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using Microsoft.VisualStudio.ExtensionManager;
using Microsoft.VisualStudio.Settings;

namespace VsixExpInstaller
{
    public class Program
    {
        private const int INSTALL_OR_UNINSTALL_SKIPPED_EXCEPTION_CODE = 1;
        private const int GLOBAL_VERSION_NEWER_EXCEPTION_CODE = -1;
        private const int INSTALL_FAILED_NOTFOUND_EXCEPTION_CODE = -2;
        private const int INSTALL_FAILED_VERSION_EXCEPTION_CODE = -3;
        private const int GLOBAL_UNINSTALL_FAILED_EXCEPTION_CODE = -4;
        private const int LOCAL_UNINSTALL_FAILED_EXCEPTION_CODE = -5;
        private const int E_ACCESSDENIED = -2147024891; // 0x80070005

        private const string ExtensionManagerCollectionPath = "ExtensionManager";

        private static string ExtractArg(List<string> args, string argName)
        {
            for (var i = 0; i < args.Count; i++)
            {
                if (args[i].StartsWith($"/{argName}:", StringComparison.OrdinalIgnoreCase) ||
                    args[i].StartsWith($"-{argName}:", StringComparison.OrdinalIgnoreCase))
                {
                    var result = args[i].Substring(argName.Length + 2);
                    args.RemoveAt(i);
                    return result;
                }
            }

            return null;
        }

        private static bool FindArg(List<string> args, string argName)
        {
            for (var i = 0; i < args.Count; i++)
            {
                if (string.Compare(args[i], $"/{argName}", StringComparison.OrdinalIgnoreCase) == 0 ||
                    string.Compare(args[i], $"-{argName}", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    args.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage: VSIXExpInstaller [/rootSuffix:suffix] [/skipIfEqualOrNewerInstalled] [/u] [/uninstallAll] [/vsInstallDir:vsInstallDir] <vsix>");
            Console.WriteLine("Suffix is Exp by default.");
        }

        private static string GetDevenvPath(string vsInstallDir)
        {
            if (string.IsNullOrWhiteSpace(vsInstallDir))
            {
                Environment.ExitCode = E_ACCESSDENIED;
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

        private static void RemoveExtensionFromPendingDeletions(WritableSettingsStore settingsStore, IExtensionHeader extensionHeader)
        {
            const string PendingDeletionsCollectionPath = ExtensionManagerCollectionPath + @"\PendingDeletions";
            var vsixToDeleteProperty = $"{extensionHeader.Identifier},{extensionHeader.Version}";

            if (settingsStore.CollectionExists(PendingDeletionsCollectionPath) &&
                settingsStore.PropertyExists(PendingDeletionsCollectionPath, vsixToDeleteProperty))
            {
                settingsStore.DeleteProperty(PendingDeletionsCollectionPath, vsixToDeleteProperty);
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

            var argList = new List<string>(args);

            var rootSuffix = ExtractArg(argList, "rootSuffix") ?? "Exp";
            var skipIfEqualOrNewerInstalled = FindArg(argList, "skipIfEqualOrNewerInstalled");
            var uninstall = FindArg(argList, "u");
            var uninstallAll = FindArg(argList, "uninstallAll");
            var printHelp = FindArg(argList, "?") || FindArg(argList, "h") || FindArg(argList, "help");
            var vsInstallDir = ExtractArg(argList, "vsInstallDir") ?? Environment.GetEnvironmentVariable("VsInstallDir");

            var expectedArgCount = uninstallAll ? 0 : 1;

            if (argList.Count != expectedArgCount)
            {
                PrintUsage();
                return 1;
            }

            string extensionPath = uninstallAll ? string.Empty : argList[0];
            string devenvPath;

            try
            {
                devenvPath = GetDevenvPath(vsInstallDir);

                var assemblyResolutionPaths = new string[] {
                    Path.Combine(vsInstallDir, @"Common7\IDE"),
                    Path.Combine(vsInstallDir, @"Common7\IDE\PrivateAssemblies"),
                    Path.Combine(vsInstallDir, @"Common7\IDE\PublicAssemblies")
                };

                AppDomain.CurrentDomain.AssemblyResolve += (object sender, ResolveEventArgs eventArgs) => {
                    var assemblyFileName = $"{eventArgs.Name.Split(',')[0]}.dll";

                    foreach (var assemblyResolutionPath in assemblyResolutionPaths)
                    {
                        var assemblyFilePath = Path.Combine(assemblyResolutionPath, assemblyFileName);

                        if (File.Exists(assemblyFilePath))
                        {
                            return Assembly.LoadFrom(assemblyFilePath);
                        }
                    }

                    return null;
                };

                Console.WriteLine(Environment.CommandLine);

                if (IsRunningAsAdmin())
                {
                    Console.WriteLine("  Running as Admin.");
                }

                RunProgram();

                // Move all of this into a local method so that it only causes the assembly loads after the resolver has been hooked up
                void RunProgram()
                {
                    var installedExtensionComparer = Comparer<IInstalledExtension>.Create((left, right) =>
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
                    });

                    using (var settingsManager = ExternalSettingsManager.CreateForApplication(devenvPath, rootSuffix))
                    {
                        ExtensionManagerService extensionManagerService = null;

                        try
                        {
                            extensionManagerService = new ExtensionManagerService(settingsManager);

                            if (uninstallAll)
                            {
                                Console.WriteLine("  Uninstalling all local extensions...");
                                UninstallAll();
                            }
                            else
                            {
                                var extensionManager = (IVsExtensionManager)(extensionManagerService);
                                var installableExtension = extensionManager.CreateInstallableExtension(extensionPath);

                                var status = GetInstallStatus(installableExtension.Header);
                                var installedVersionIsEqualOrNewer = installableExtension.Header.Version < status.installedExtension?.Header.Version;

                                if (uninstall)
                                {
                                    if (status.installed)
                                    {
                                        if (installedVersionIsEqualOrNewer && skipIfEqualOrNewerInstalled)
                                        {
                                            Environment.ExitCode = INSTALL_OR_UNINSTALL_SKIPPED_EXCEPTION_CODE;
                                            throw new Exception($"Skipping uninstall of version ({status.installedExtension.Header.Version}), which is equal to or newer than the one supplied on the command line ({installableExtension.Header.Version}).");
                                        }

                                        if (status.installedGlobally)
                                        {
                                            Console.WriteLine($"  Skipping uninstall for global extension: '{status.installedExtension.InstallPath}'");
                                        }
                                        else
                                        {
                                            Console.WriteLine($"  Uninstalling local extension: '{status.installedExtension.InstallPath}'");
                                            Uninstall(status.installedExtension);
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine("  Nothing to uninstall...");
                                    }
                                }
                                else
                                {
                                    if (status.installed)
                                    {
                                        if (installedVersionIsEqualOrNewer && skipIfEqualOrNewerInstalled)
                                        {
                                            Environment.ExitCode = INSTALL_OR_UNINSTALL_SKIPPED_EXCEPTION_CODE;
                                            throw new Exception($"Skipping install of version ({installableExtension.Header.Version}), which is older than the one currently installed ({status.installedExtension.Header.Version}).");
                                        }

                                        if (status.installedGlobally)
                                        {
                                            if (installedVersionIsEqualOrNewer)
                                            {
                                                Environment.ExitCode = GLOBAL_VERSION_NEWER_EXCEPTION_CODE;
                                                throw new Exception($"The version you are attempting to install ({installableExtension.Header.Version}) has a version that is less than the one installed globally ({status.installedExtension.Header.Version}).");
                                            }
                                            else
                                            {
                                                Console.WriteLine($"  Installing local extension ({extensionPath}) over global extension ({status.installedExtension.InstallPath})");
                                            }
                                        }
                                        else
                                        {
                                            Console.WriteLine($"  Updating local extension ({status.installedExtension.InstallPath}) to '{extensionPath}'");
                                            Uninstall(status.installedExtension);
                                        }
                                        
                                    }
                                    else
                                    {
                                        Console.WriteLine($"  Installing local extension: '{extensionPath}'");
                                    }

                                    Install(installableExtension);
                                }
                            }
                        }
                        finally
                        {
                            extensionManagerService?.Close();
                            extensionManagerService = null;
                        }

                        bool IsInstalledGlobally(IInstalledExtension installedExtension)
                        {
                            return installedExtension.InstalledPerMachine || installedExtension.InstallPath.StartsWith(vsInstallDir, StringComparison.OrdinalIgnoreCase);
                        }

                        (bool installed, bool installedGlobally, IInstalledExtension installedExtension) GetInstallStatus(IExtensionHeader extensionHeader)
                        {
                            var installed = extensionManagerService.TryGetInstalledExtension(extensionHeader.Identifier, out var installedExtension);
                            var installedGlobally = installed && IsInstalledGlobally(installedExtension);
                            return (installed, installedGlobally, installedExtension);
                        }

                        void Install(IInstallableExtension installableExtension)
                        {
                            extensionManagerService.Install(installableExtension, perMachine: false);
                            var settingsStore = settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);

                            EnableLoadingAllExtensions(settingsStore);
                            RemoveExtensionFromPendingDeletions(settingsStore, installableExtension.Header);
                            UpdateLastExtensionsChange(settingsStore);

                            // Recreate the extensionManagerService to force the extension cache to recreate
                            extensionManagerService?.Close();
                            extensionManagerService = new ExtensionManagerService(settingsManager);

                            var status = GetInstallStatus(installableExtension.Header);

                            if (!status.installed)
                            {
                                Environment.ExitCode = INSTALL_FAILED_NOTFOUND_EXCEPTION_CODE;
                                throw new Exception($"The extension failed to install. It could not be located.");
                            }
                            else if (status.installedExtension.Header.Version != installableExtension.Header.Version)
                            {
                                Environment.ExitCode = INSTALL_FAILED_VERSION_EXCEPTION_CODE;
                                throw new Exception($"The extension failed to install. The located version ({status.installedExtension.Header.Version}) does not match the expected version ({installableExtension.Header.Version}).");
                            }
                            else if (status.installedGlobally)
                            {
                                Console.WriteLine($"    The extension was succesfully installed globally: '{status.installedExtension.InstallPath}'");
                            }
                            else
                            {
                                Console.WriteLine($"    The extension was succesfully installed locally: '{status.installedExtension.InstallPath}'");
                            }
                        }

                        void Uninstall(IInstalledExtension installedExtension)
                        {
                            extensionManagerService.Uninstall(installedExtension);

                            // Recreate the extensionManagerService to force the extension cache to recreate
                            extensionManagerService?.Close();
                            extensionManagerService = new ExtensionManagerService(settingsManager);

                            var status = GetInstallStatus(installedExtension.Header);

                            if (status.installed)
                            {
                                var wasInstalledGlobally = IsInstalledGlobally(installedExtension);

                                if (wasInstalledGlobally)
                                {
                                    // We should never hit this case, as we shouldn't be passing in gobally installed extensions

                                    if (status.installedGlobally)
                                    {
                                        Environment.ExitCode = GLOBAL_UNINSTALL_FAILED_EXCEPTION_CODE;
                                        throw new Exception("The global extension failed to uninstall.");
                                    }
                                    else
                                    {
                                        // This should be impossible even if we tried to uninstall a global extension...
                                        Environment.ExitCode = LOCAL_UNINSTALL_FAILED_EXCEPTION_CODE;
                                        throw new Exception($"The global extension was uninstalled. A local extension still exists: '{status.installedExtension.InstallPath}'");
                                    }
                                }
                                else if (status.installedGlobally)
                                {
                                    Console.WriteLine($"    The local extension was succesfully uninstalled. A global extension still exists: '{status.installedExtension.InstallPath}'");
                                }
                                else
                                {
                                    throw new Exception("The local extension failed to uninstall.");
                                }
                            }
                            else
                            {
                                Console.WriteLine("    The local extension was succesfully uninstalled.");
                            }
                        }

                        void UninstallAll()
                        {
                            // We only want extensions which are not installed per machine and we want them sorted by their dependencies.
                            var installedExtensions = extensionManagerService.GetInstalledExtensions()
                                                                             .Where((installedExtension) => !IsInstalledGlobally(installedExtension))
                                                                             .OrderBy((installedExtension) => installedExtension, installedExtensionComparer);

                            foreach (var installedExtension in installedExtensions)
                            {
                                Console.WriteLine($"    Uninstalling local extension: '{installedExtension.InstallPath}'");
                                Uninstall(installedExtension);
                            }
                        }
                    }
                }
            }
            catch (Exception e) when (Environment.ExitCode != 0)
            {
                if (Environment.ExitCode < 0)
                {
                    Console.Error.WriteLine(e);
                }
                else
                {
                    Console.WriteLine("  " + e.Message);
                }
            }

            return Environment.ExitCode;
        }
    }
}
