// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis.CodeActions;

namespace BuildActionTelemetryTable
{
    class Program
    {
        private static readonly string s_executingPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        private static readonly string[] s_excludeList = new[]
        {
            "Microsoft.CodeAnalysis.AnalyzerUtilities.dll"
        };

        static void Main(string[] args)
        {
            var assemblies = GetAssemblies(args);
            var codeActionTypes = GetCodeActionTypes(assemblies);
            var telemetryInfos = codeActionTypes.Select(type => GetTelemetryInfo(type));

            var hashes = new StringBuilder();

            hashes.AppendLine("let actions = datatable(ActionName: string, Prefix: string, Suffix: string)");

            hashes.AppendLine("[");

            foreach (var (ActionTypeName, Prefix, Suffix) in telemetryInfos)
            {
                hashes.AppendLine(@$"  ""{ActionTypeName}"", ""{Prefix}"", ""{Suffix}"",");
            }

            hashes.Append("];");

            File.WriteAllText("ActionTable.txt", hashes.ToString());
        }

        internal static ImmutableArray<Assembly> GetAssemblies(string[] paths)
        {
            if (paths.Length == 0)
            {
                // By default inspect the Roslyn assemblies
                paths = Directory.EnumerateFiles(s_executingPath, "Microsoft.CodeAnalysis*.dll")
                    .Where(path => !s_excludeList.Any(exclude => path.EndsWith(exclude)))
                    .ToArray();
            }

            return paths.Select(path => Assembly.LoadFrom(path))
                .ToImmutableArray();
        }

        internal static ImmutableArray<Type> GetCodeActionTypes(IEnumerable<Assembly> assemblies)
        {
            var types = assemblies.SelectMany(
                assembly => assembly.GetTypes().Where(
                    type => !type.GetTypeInfo().IsInterface && !type.GetTypeInfo().IsAbstract));

            return types
                .Where(t => typeof(CodeAction).IsAssignableFrom(t))
                .ToImmutableArray();
        }

        internal static (string ActionTypeName, string Prefix, string Suffix) GetTelemetryInfo(Type type, short scope = 0)
        {
            type = GetTypeForTelemetry(type);

            // AssemblyQualifiedName will change across version numbers, FullName won't

            // GetHashCode on string is not stable. From documentation:
            // The hash code itself is not guaranteed to be stable.
            // Hash codes for identical strings can differ across .NET implementations, across .NET versions,
            // and across .NET platforms (such as 32-bit and 64-bit) for a single version of .NET. In some cases,
            // they can even differ by application domain.
            // This implies that two subsequent runs of the same program may return different hash codes.
            //
            // As such, we keep the original prefix that was being used for legacy purposes, but
            // use a stable hashing algorithm (FNV) that doesn't depend on platform
            // or .NET implementation. We can map the prefix across legacy versions, but
            // as we support more platforms and variations of builds the suffix will be constant
            // and usable
            var prefix = type.FullName.GetHashCode();
            var suffix = Roslyn.Utilities.Hash.GetFNVHashCode(type.FullName);

            // Suffix is the remaining 8 bytes, and the hash code only makes up 4. Pad
            // the remainder with an empty byte array
            var suffixBytes = BitConverter.GetBytes(suffix).Concat(new byte[4]).ToArray();
            var telemetryId = new Guid(prefix, scope, 0, suffixBytes).ToString();

            return (type.FullName, telemetryId.Substring(0, 8), telemetryId.Substring(19));
        }

        internal static Type GetTypeForTelemetry(Type type)
            => type.IsConstructedGenericType ? type.GetGenericTypeDefinition() : type;

        internal static short GetScopeIdForTelemetry(FixAllScope scope)
            => (short)(scope switch
            {
                FixAllScope.Document => 1,
                FixAllScope.Project => 2,
                FixAllScope.Solution => 3,
                _ => 4,
            });

        internal enum FixAllScope
        {
            None,
            Document,
            Project,
            Solution
        }
    }
}
