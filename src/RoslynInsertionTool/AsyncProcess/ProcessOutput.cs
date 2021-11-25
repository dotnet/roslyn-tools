// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace System.Threading.Tasks
{
    [Serializable]
    public sealed class ProcessOutput : ISerializable
    {
        public ProcessOutput(int exitCode, IEnumerable<string> outputLines, IEnumerable<string> errorLines)
        {
            ExitCode = exitCode;
            OutputLines = outputLines.ToList();
            ErrorLines = errorLines.ToList();
        }

        private ProcessOutput(SerializationInfo info, StreamingContext context)
        {
            ExitCode = info.GetInt32(nameof(ExitCode));
            OutputLines = info.GetValue(nameof(OutputLines), typeof(List<string>)) as List<string>;
            ErrorLines = info.GetValue(nameof(ErrorLines), typeof(List<string>)) as List<string>;
        }

        public int ExitCode { get; }

        public List<string> OutputLines { get; }

        public List<string> ErrorLines { get; }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            info.AddValue(nameof(ExitCode), ExitCode);
            info.AddValue(nameof(OutputLines), OutputLines, typeof(List<string>));
            info.AddValue(nameof(ErrorLines), ErrorLines, typeof(List<string>));
        }
    }
}
