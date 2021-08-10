// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Text;

namespace roslyn.optprof.lib
{
    public class Vsix : IDisposable
    {
        private IVsixPackage _package;

        public Vsix(IVsixPackage package)
        {
            _package = package;
        }

        public static Vsix Create(string pathToVsix)
        {
            var package = new VsixPackage(pathToVsix);
            return new Vsix(package);
        }

        public JObject ParseJsonManifest(string pathToManifest)
        {
            using (var stream = _package.GetStream(pathToManifest))
            {
                string jsonStr = stream.ReadToEnd();
                return JObject.Parse(jsonStr);
            }
        }

        public void Dispose()
        {
            ((IDisposable)_package).Dispose();
        }
    }
}
