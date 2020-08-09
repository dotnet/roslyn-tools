namespace CreateTagsForVSRelease
{
    internal sealed class RoslynBuildInformation
    {
        public readonly string CommitSha;
        public readonly string SourceBranch;
        public readonly string BuildId;
        public readonly string? NugetPackageVersion;

        public RoslynBuildInformation(string commitSha, string sourceBranch, string buildId, string? nugetPackageVersion)
        {
            CommitSha = commitSha;
            SourceBranch = sourceBranch;
            BuildId = buildId;
            NugetPackageVersion = nugetPackageVersion;
        }
    }
}
