using System;
using System.IO;
using System.IO.Packaging;

namespace roslyn.optprof.lib
{
    public class VsixPackage : IVsixPackage, IDisposable
    {
        private readonly Package _package;

        public VsixPackage(string pathToVsix)
        {
            _package = Package.Open(pathToVsix);
        }

        public Stream GetStream(string relativePath)
        {
            var uri = new Uri(relativePath, UriKind.Relative);
            var part = _package.GetPart(uri);
            return part.GetStream(FileMode.Open);
        }

        public void Dispose()
        {
            ((IDisposable)_package).Dispose();
        }
    }
}
