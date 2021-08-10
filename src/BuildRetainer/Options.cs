// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System.Linq;

namespace BuildRetainer
{
    internal class Options
    {
        public string BuildQueueName { get; set; }
        public string ComponentName { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public bool IsValid => new[] { BuildQueueName, ComponentName }.All(s => !string.IsNullOrEmpty(s));
    }
}
