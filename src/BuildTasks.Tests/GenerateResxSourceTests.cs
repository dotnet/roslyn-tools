// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System;
using Xunit;

namespace RoslynTools.BuildTasks.UnitTests
{
    public class GenerateResxSourceTests
    {
        [Fact]
        public void Errors_ResourceName()
        {
            var engine = new MockEngine();

            var task = new GenerateResxSource
            {
                BuildEngine = engine
            };

            bool result = task.Execute();
            Assert.Equal("ERROR : ResourceName not specified" + Environment.NewLine, engine.Log);
            Assert.False(result);
        }
    }
}
