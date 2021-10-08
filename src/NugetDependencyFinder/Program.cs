using System.Text.RegularExpressions;
using NuGet.Common;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;

var (verbose, packageFolder) = ParseCommandLine();

if (packageFolder is null)
{
    Console.WriteLine("Usage: NugetDependencyFinder [-v] <nupkg directory>");
    return;
}

var packages = from file in Directory.EnumerateFiles(packageFolder, "*.nupkg")
               let fileName = Path.GetFileName(file)
               let regex = Regex.Match(fileName, @"^(.*?)\.((?:\.?[0-9]+){3,}(?:[-a-z]+)?)\.nupkg$")
               where regex.Success
               select regex.Groups[1].Value;

var logger = NullLogger.Instance;
var cache = new SourceCacheContext();

var nugetOrg = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
var nugetOrgFinder = await nugetOrg.GetResourceAsync<FindPackageByIdResource>().ConfigureAwait(false);

await foreach (var dependency in GetAllDependenciesAsync())
{
    if (packages.Contains(dependency.Id))
    {
        Write(dependency, "One of our packages, so versioned uniformly.", verboseOnly: true);
        continue;
    }
    else if (!dependency.VersionRange.MinVersion.IsPrerelease)
    {
        Write(dependency, "Already using a released version.", verboseOnly: true);
        continue;
    }

    if (!await nugetOrgFinder.DoesPackageExistAsync(dependency.Id, dependency.VersionRange.MinVersion, cache, logger, CancellationToken.None).ConfigureAwait(false))
    {
        Write(dependency, "Doesn't exist on nuget.org, nothing to do.");
        continue;
    }

    var versions = await nugetOrgFinder.GetAllVersionsAsync(dependency.Id, cache, logger, CancellationToken.None).ConfigureAwait(false);

    var desiredVersion = (from v in versions
                          where !v.IsPrerelease
                          where v.Version > dependency.VersionRange.MinVersion.Version
                          select v).FirstOrDefault();

    if (desiredVersion is null)
    {
        Write(dependency, "No released version to upgrade to on nuget.org.", ConsoleColor.Cyan);
        continue;
    }

    Write(dependency, $"Upgrade to {desiredVersion}.", ConsoleColor.Yellow);
}

void Write(PackageDependency dependency, string message, ConsoleColor? color = null, bool verboseOnly = false)
{
    if (verboseOnly && !verbose)
    {
        return;
    }

    Console.Write($"{dependency.Id}, {dependency.VersionRange.MinVersion}: ");
    if (color is not null)
    {
        Console.ForegroundColor = color.Value;
    }
    Console.Write(message);
    Console.ResetColor();

    Console.WriteLine();
}

async IAsyncEnumerable<PackageDependency> GetAllDependenciesAsync()
{
    var local = Repository.Factory.GetCoreV3(packageFolder);
    var localFinder = await local.GetResourceAsync<FindPackageByIdResource>().ConfigureAwait(false);
    var foundPackages = new HashSet<string>();

    foreach (var package in packages)
    {
        var localVersions = await localFinder.GetAllVersionsAsync(package, cache, logger, CancellationToken.None).ConfigureAwait(false);
        var dependencies = await localFinder.GetDependencyInfoAsync(package, localVersions.First(), cache, logger, CancellationToken.None).ConfigureAwait(false);

        foreach (var group in dependencies.DependencyGroups)
        {
            foreach (var dependency in group.Packages)
            {
                if (foundPackages.Add(dependency.Id))
                {
                    yield return dependency;
                }
            }
        }
    }
}

(bool, string?) ParseCommandLine()
{
    if (args.Length == 0)
    {
        return (false, null);
    }

    if (args[0] == "-v")
    {
        if (args.Length == 1)
        {
            return (false, null);
        }

        return (true, args[1]);
    }

    return (false, args[0]);
}
