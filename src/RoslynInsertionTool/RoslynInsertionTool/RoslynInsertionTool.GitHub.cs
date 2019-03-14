using System;
using System.Collections.Generic;

using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Roslyn.Insertion
{
    static partial class RoslynInsertionTool
    {
        public partial class GitHubComparison
        {
            [JsonProperty("url")]
            public Uri Url { get; set; }

            [JsonProperty("html_url")]
            public Uri HtmlUrl { get; set; }

            [JsonProperty("permalink_url")]
            public Uri PermalinkUrl { get; set; }

            [JsonProperty("diff_url")]
            public Uri DiffUrl { get; set; }

            [JsonProperty("patch_url")]
            public Uri PatchUrl { get; set; }

            [JsonProperty("base_commit")]
            public GitHubCommit BaseCommit { get; set; }

            [JsonProperty("merge_base_commit")]
            public GitHubCommit MergeBaseCommit { get; set; }

            [JsonProperty("status")]
            public string Status { get; set; }

            [JsonProperty("ahead_by")]
            public long AheadBy { get; set; }

            [JsonProperty("behind_by")]
            public long BehindBy { get; set; }

            [JsonProperty("total_commits")]
            public long TotalCommits { get; set; }

            [JsonProperty("commits")]
            public GitHubCommit[] Commits { get; set; }

            [JsonProperty("files")]
            public GitFile[] Files { get; set; }
        }

        public partial class GitHubCommit
        {
            [JsonProperty("url")]
            public Uri Url { get; set; }

            [JsonProperty("sha")]
            public string Sha { get; set; }

            [JsonProperty("node_id")]
            public string NodeId { get; set; }

            [JsonProperty("html_url")]
            public Uri HtmlUrl { get; set; }

            [JsonProperty("comments_url")]
            public Uri CommentsUrl { get; set; }

            [JsonProperty("commit")]
            public GitCommit Commit { get; set; }

            [JsonProperty("author")]
            public GitHubAuthor Author { get; set; }

            [JsonProperty("committer")]
            public GitHubAuthor Committer { get; set; }

            [JsonProperty("parents")]
            public GitTree[] Parents { get; set; }
        }

        public partial class GitHubAuthor
        {
            [JsonProperty("login")]
            public string Login { get; set; }

            [JsonProperty("id")]
            public long Id { get; set; }

            [JsonProperty("node_id")]
            public string NodeId { get; set; }

            [JsonProperty("avatar_url")]
            public Uri AvatarUrl { get; set; }

            [JsonProperty("gravatar_id")]
            public string GravatarId { get; set; }

            [JsonProperty("url")]
            public Uri Url { get; set; }

            [JsonProperty("html_url")]
            public Uri HtmlUrl { get; set; }

            [JsonProperty("followers_url")]
            public Uri FollowersUrl { get; set; }

            [JsonProperty("following_url")]
            public string FollowingUrl { get; set; }

            [JsonProperty("gists_url")]
            public string GistsUrl { get; set; }

            [JsonProperty("starred_url")]
            public string StarredUrl { get; set; }

            [JsonProperty("subscriptions_url")]
            public Uri SubscriptionsUrl { get; set; }

            [JsonProperty("organizations_url")]
            public Uri OrganizationsUrl { get; set; }

            [JsonProperty("repos_url")]
            public Uri ReposUrl { get; set; }

            [JsonProperty("events_url")]
            public string EventsUrl { get; set; }

            [JsonProperty("received_events_url")]
            public Uri ReceivedEventsUrl { get; set; }

            [JsonProperty("type")]
            public string Type { get; set; }

            [JsonProperty("site_admin")]
            public bool SiteAdmin { get; set; }
        }

        public partial class GitCommit
        {
            [JsonProperty("url")]
            public Uri Url { get; set; }

            [JsonProperty("author")]
            public GitAuthor Author { get; set; }

            [JsonProperty("committer")]
            public GitAuthor Committer { get; set; }

            [JsonProperty("message")]
            public string Message { get; set; }

            [JsonProperty("tree")]
            public GitTree Tree { get; set; }

            [JsonProperty("comment_count")]
            public long CommentCount { get; set; }

            [JsonProperty("verification")]
            public GitVerification Verification { get; set; }
        }

        public partial class GitAuthor
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("email")]
            public string Email { get; set; }

            [JsonProperty("date")]
            public DateTimeOffset Date { get; set; }
        }

        public partial class GitTree
        {
            [JsonProperty("url")]
            public Uri Url { get; set; }

            [JsonProperty("sha")]
            public string Sha { get; set; }
        }

        public partial class GitVerification
        {
            [JsonProperty("verified")]
            public bool Verified { get; set; }

            [JsonProperty("reason")]
            public string Reason { get; set; }

            [JsonProperty("signature")]
            public object Signature { get; set; }

            [JsonProperty("payload")]
            public object Payload { get; set; }
        }

        public partial class GitFile
        {
            [JsonProperty("sha")]
            public string Sha { get; set; }

            [JsonProperty("filename")]
            public string Filename { get; set; }

            [JsonProperty("status")]
            public string Status { get; set; }

            [JsonProperty("additions")]
            public long Additions { get; set; }

            [JsonProperty("deletions")]
            public long Deletions { get; set; }

            [JsonProperty("changes")]
            public long Changes { get; set; }

            [JsonProperty("blob_url")]
            public Uri BlobUrl { get; set; }

            [JsonProperty("raw_url")]
            public Uri RawUrl { get; set; }

            [JsonProperty("contents_url")]
            public Uri ContentsUrl { get; set; }

            [JsonProperty("patch")]
            public string Patch { get; set; }
        }

        public partial class GitHubComparison
        {
            public static GitHubComparison FromJson(string json) => JsonConvert.DeserializeObject<GitHubComparison>(json, Converter.Settings);
        }

        internal static class Converter
        {
            public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
            {
                MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
                DateParseHandling = DateParseHandling.None,
                Converters =
                {
                    new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
                },
            };
        }
    }
}
