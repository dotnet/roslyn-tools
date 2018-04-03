// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
