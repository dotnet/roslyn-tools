// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignTool
{
    internal static partial class SignToolFactory
    {
        /// <summary>
        /// The <see cref="SignToolBase"/> implementation used for test / validation runs.  Does not actually 
        /// change the sign state of the binaries.
        /// </summary>
        private sealed class TestSignTool : SignToolBase
        {
            internal TestSignTool(SignToolArgs args) : base(args)
            {

            }

            public override void RemovePublicSign(string assemblyPath)
            {

            }

            public override bool VerifySignedAssembly(Stream assemblyStream)
            {
                return true;
            }

            protected override int RunMSBuild(ProcessStartInfo startInfo, TextWriter textWriter)
            {
                return 0;
            }
        }
    }
}
