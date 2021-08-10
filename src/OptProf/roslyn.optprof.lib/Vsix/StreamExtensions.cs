// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.
using System.IO;
using System.Text;

namespace roslyn.optprof.lib
{
    public static class StreamExtensions
    {
        public static string ReadToEnd(this Stream stream)
        {
            string result;
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 2048, leaveOpen: true))
            {
                result = reader.ReadToEnd();
            }

            return result;
        }
    }
}
