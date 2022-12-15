using NuGet.ProjectModel;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Packaging.Core;

namespace ProjectDependencies
{
    public static class BuildDependencyFinder
    {
        static readonly LockFileFormat s_lockFileFormat = new();

        public static async Task<ImmutableArray<DependencyNode>> FindDependenciesAsync(string folder, string packageName, string packageVersion, Action<int, bool> callback, CancellationToken cancellationToken)
        {
            var collectionOfLockFiles = new ConcurrentDictionary<string, LockFile>();

            var fileCount = 0;
            foreach (var file in Directory.EnumerateFiles(folder, "project.assets.json", SearchOption.AllDirectories))
            {
                await LoadLockFileAsync(file, collectionOfLockFiles);
                callback(++fileCount, false);
            }

            callback(fileCount, true);

            var roots = await BuildReversedDependencyGraphAsync(collectionOfLockFiles.ToImmutableDictionary(), packageName, packageVersion, cancellationToken);

            return roots;
        }

        private static Task<ImmutableArray<DependencyNode>> BuildReversedDependencyGraphAsync(ImmutableDictionary<string, LockFile> lockFiles, string packageName, string packageVersion, CancellationToken cancellationToken)
        {
            // Find all the initial version nodes for the package
            var rootNodes = new List<DependencyNode>();

            foreach (var (fileName, lockFile) in lockFiles)
            {
                foreach (var target in lockFile.Targets)
                {
                    foreach (var library in target.Libraries)
                    {
                        foreach (var dependency in library.Dependencies)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            if (dependency.Id.Equals(packageName, StringComparison.OrdinalIgnoreCase) && dependency.VersionRange.OriginalString == packageVersion)
                            {
                                if (TryGetMatchingDependency(rootNodes, dependency, out var dependencyNode))
                                {
                                    dependencyNode.EnsureDependentNode(library);
                                    break;
                                }

                                rootNodes.Add(new(dependency.Id, dependency.VersionRange.OriginalString, "Dependency", null));
                            }
                        }
                    }
                }
            }

            var finalRoots = rootNodes.ToImmutableArray();

            // Now that we have the roots, we need to span out and fill in the dependency chains. For each root
            // we'll kick up a thread to do this
            Parallel.ForEach(finalRoots, (node) =>
            {
                var nodesToTraverse = new Stack<DependencyNode>(node.Children);
                while (nodesToTraverse.TryPop(out var currentNode))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    FindDependentLibraries(currentNode, lockFiles);
                    foreach (var nodeToAdd in currentNode.Children)
                    {
                        nodesToTraverse.Push(nodeToAdd);
                    }
                }
            });

            return Task.FromResult(finalRoots);
        }

        private static void FindDependentLibraries(DependencyNode currentNode, ImmutableDictionary<string, LockFile> lockFiles)
        {
            foreach (var (fileName, lockFile) in lockFiles)
            {
                foreach (var target in lockFile.Targets)
                {
                    foreach (var library in target.Libraries)
                    {
                        foreach (var dependency in library.Dependencies)
                        {
                            if (dependency.VersionRange.OriginalString == currentNode.Version &&
                                dependency.Id == currentNode.Name)
                            {
                                currentNode.EnsureDependentNode(library);
                                break;
                            }
                        }
                    }
                }
            }
        }

        private static bool TryGetMatchingDependency(List<DependencyNode> rootNodes, PackageDependency dependency, [NotNullWhen(true)] out DependencyNode? dependencyNode)
        {
            foreach (var node in rootNodes)
            {
                if (node.Version == dependency.VersionRange.OriginalString &&
                    node.Name == dependency.Id)
                {
                    dependencyNode = node;
                    return true;
                }
            }

            dependencyNode = null;
            return false;
        }

        private static async Task LoadLockFileAsync(string file, ConcurrentDictionary<string, LockFile> collectionOfLockFiles)
        {
            var text = await File.ReadAllTextAsync(file);
            var lockFile = s_lockFileFormat.Parse(text, $"In Memory: {file}");
            if (!collectionOfLockFiles.TryAdd(file, lockFile))
            {
                throw new InvalidOperationException();
            }
        }
    }
}
