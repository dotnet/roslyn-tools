// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignTool
{
    internal interface ISignTool
    {
        void RemovePublicSign(string assemblyPath);

        bool VerifySignedAssembly(Stream assemblyStream);

        void Sign(int round, IEnumerable<FileSignInfo> filesToSign, TextWriter textWriter);
    }

    internal static partial class SignToolFactory
    {
        internal static ISignTool Create(SignToolArgs args)
        {
            if (args.Test)
            {
                return new TestSignTool(args);
            }

            return new RealSignTool(args);
        }
    }
}
