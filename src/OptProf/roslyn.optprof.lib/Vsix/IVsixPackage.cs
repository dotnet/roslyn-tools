using System;
using System.IO;

namespace roslyn.optprof.lib
{
    public interface IVsixPackage
    {
        Stream GetStream(string relativePath);
    }
}
