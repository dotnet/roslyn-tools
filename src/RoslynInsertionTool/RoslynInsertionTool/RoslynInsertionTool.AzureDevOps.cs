using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Roslyn.Insertion
{
    static partial class RoslynInsertionTool
    {
        private static readonly Regex IsMergedPRCommit = new Regex(@"^Merged PR (\d+):");

        internal static async Task<(IEnumerable<Commit>, string)> GetAzureDevOpsMergeCommitsAndDiffUrlAsync(string fromSha, string toSha, string fromUrl)
        {
            var from = fromSha.Substring(0, 8);
            var to = toSha.Substring(0, 8);
            var organization = fromUrl.Split('/')[3];
            var repo = fromUrl.Split('/')[4];

            var azureDevOpsClient = new HttpClient();
            var commitSearchUrl = $@"https://devdiv.visualstudio.com/DevDiv/_apis/git/repositories/{repo}/commits?searchCriteria.itemVersion.version={fromSha}&searchCriteria.itemVersion.versionType=commit&searchCriteria.itemVersion.versionOptions=previousChange&searchCriteria.compareVersion.version={toSha}&searchCriteria.compareVersion.versionType=commit&api-version=5.0";
            var commitSearchJson = await azureDevOpsClient.GetStringAsync(commitSearchUrl);
            var comparison = AzureDevOpsCommitSearch.FromJson(commitSearchJson);

            return (comparison.Value.Where(isPRMerge).Select(CreateCommit), "");

            bool isPRMerge(AzureDevOpsCommit commit) => IsMergedPRCommit.Match(commit.Comment).Success;

            Commit CreateCommit(AzureDevOpsCommit azdoCommit)
            {
                return new Commit
                {
                    Sha = azdoCommit.CommitId,
                    Author = azdoCommit.Author.Name,
                    Date = azdoCommit.Author.Date.Date,
                    Message = azdoCommit.Comment,
                    Url = azdoCommit.Url
                };
            }
        }

        private partial class AzureDevOpsCommitSearch
        {
            [JsonProperty("count")]
            public long Count { get; set; }

            [JsonProperty("value")]
            public AzureDevOpsCommit[] Value { get; set; }
        }

        private class AzureDevOpsCommit
        {
            [JsonProperty("commitId")]
            public string CommitId { get; set; }

            [JsonProperty("author")]
            public AzureDevOpsAuthor Author { get; set; }

            [JsonProperty("committer")]
            public AzureDevOpsAuthor Committer { get; set; }

            [JsonProperty("comment")]
            public string Comment { get; set; }

            [JsonProperty("commentTruncated", NullValueHandling = NullValueHandling.Ignore)]
            public bool? CommentTruncated { get; set; }

            [JsonProperty("changeCounts")]
            public AzureDevOpsChangeCounts ChangeCounts { get; set; }

            [JsonProperty("url")]
            public Uri Url { get; set; }

            [JsonProperty("remoteUrl")]
            public Uri RemoteUrl { get; set; }
        }

        private class AzureDevOpsAuthor
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("email")]
            public string Email { get; set; }

            [JsonProperty("date")]
            public DateTimeOffset Date { get; set; }
        }

        private class AzureDevOpsChangeCounts
        {
            [JsonProperty("Add")]
            public long Add { get; set; }

            [JsonProperty("Edit")]
            public long Edit { get; set; }

            [JsonProperty("Delete")]
            public long Delete { get; set; }
        }

        private partial class AzureDevOpsCommitSearch
        {
            public static AzureDevOpsCommitSearch FromJson(string json) => JsonConvert.DeserializeObject<AzureDevOpsCommitSearch>(json, AzureDevOpsConverter.Settings);
        }

        internal static class AzureDevOpsConverter
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
