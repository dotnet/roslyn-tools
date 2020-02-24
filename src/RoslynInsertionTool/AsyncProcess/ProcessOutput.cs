// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
