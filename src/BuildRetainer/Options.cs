// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
