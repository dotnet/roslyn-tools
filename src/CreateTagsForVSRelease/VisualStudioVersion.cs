using System;

namespace CreateTagsForVSRelease
{
    public struct VisualStudioVersion
    {
        public readonly string MainVersion;
        public readonly string? PreviewVersion;
        public readonly string CommitSha;
        public readonly DateTime CreationTime;
        public readonly string BuildId;

        public VisualStudioVersion(string mainVersion, string? previewVersion, string commitSha, DateTime creationTime, string buildId)
        {
            MainVersion = mainVersion;
            PreviewVersion = previewVersion;
            CommitSha = commitSha;
            CreationTime = creationTime;
            BuildId = buildId;
        }
    }
}
