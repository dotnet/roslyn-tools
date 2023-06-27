using NuGet.ProjectModel;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Packaging.Core;
using System.Threading;
using System.IO;

namespace ProjectDependencies
{
    public static class BuildDependencyFinder
    {
        static readonly LockFileFormat s_lockFileFormat = new();

        /// <summary>
        /// Uses project.assets.json files in the given folder to search for dependency graphs related to <paramref name="packageName"/> with version <paramref name="packageVersion"/>.
        /// Note that it does an exact match for both and is not particularly optimized for speed. It recursively looks through all loaded files until it finds a root. 
        /// </summary>
        public static DependencyNode[] FindDependencies(string folder, string packageName, string packageVersion, Action<int, bool> callback, CancellationToken cancellationToken)
        {
            var collectionOfLockFiles = new Dictionary<string, LockFile>();

            var fileCount = 0;
            foreach (var file in Directory.EnumerateFiles(folder, "project.assets.json", SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();
                LoadLockFile(file, collectionOfLockFiles);
                callback(++fileCount, false);
            }

            callback(fileCount, true);

            var roots = BuildReversedDependencyGraph(collectionOfLockFiles, packageName, packageVersion, cancellationToken);

            return roots;
        }

        private static DependencyNode[] BuildReversedDependencyGraph(IReadOnlyDictionary<string, LockFile> lockFiles, string packageName, string packageVersion, CancellationToken cancellationToken)
        {
            // Find all the initial version nodes for the package
            var rootNodes = new List<DependencyNode>();

            foreach (var pair in lockFiles)
            {
                var (fileName, lockFile) = (pair.Key, pair.Value);

                foreach (var target in lockFile.Targets)
                {
                    foreach (var library in target.Libraries)
                    {
                        foreach (var dependency in library.Dependencies)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            if (dependency.Id.Equals(packageName, StringComparison.OrdinalIgnoreCase) && (dependency.VersionRange.OriginalString == packageVersion || packageVersion.Length == 0))
                            {
                                if (TryGetMatchingDependency(rootNodes, dependency, out var dependencyNode))
                                {
                                    dependencyNode!.EnsureDependentNode(library);
                                    break;
                                }

                                rootNodes.Add(new(dependency.Id, dependency.VersionRange.OriginalString, "Dependency", null));
                            }
                        }
                    }
                }
            }

            var finalRoots = rootNodes.ToArray();

            // Now that we have the roots, we need to span out and fill in the dependency chains. For each root
            // we'll kick up a thread to do this
            Parallel.ForEach(finalRoots, (node) =>
            {
                var nodesToTraverse = new Stack<DependencyNode>(node.Children);
                while (nodesToTraverse.Count > 0)
                {
                    var currentNode = nodesToTraverse.Pop();
                    cancellationToken.ThrowIfCancellationRequested();

                    FindDependentLibraries(currentNode, lockFiles);
                    foreach (var nodeToAdd in currentNode.Children)
                    {
                        nodesToTraverse.Push(nodeToAdd);
                    }
                }
            });

            return finalRoots;
        }

        private static void FindDependentLibraries(DependencyNode currentNode, IReadOnlyDictionary<string, LockFile> lockFiles)
        {
            foreach (var pair in lockFiles)
            {
                var (fileName, lockFile) = (pair.Key, pair.Value);

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

        private static bool TryGetMatchingDependency(List<DependencyNode> rootNodes, PackageDependency dependency, out DependencyNode? dependencyNode)
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

        private static void LoadLockFile(string file, Dictionary<string, LockFile> collectionOfLockFiles)
        {
            var text = File.ReadAllText(file);
            var lockFile = s_lockFileFormat.Parse(text, $"In Memory: {file}");
            collectionOfLockFiles.Add(file, lockFile);
        }
    }
}
