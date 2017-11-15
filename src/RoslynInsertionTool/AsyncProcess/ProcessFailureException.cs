// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace System.Threading.Tasks
{
    [Serializable]
    public sealed class ProcessFailureException : Exception
    {
        public ProcessFailureException(string path, string arguments, int exitCode, ProcessOutput processOutput)
        {
            Path = path;
            Arguments = arguments;
            ExitCode = exitCode;
            ProcessOutput = processOutput;
        }

        private ProcessFailureException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            Path = info.GetString(nameof(Path));
            Arguments = info.GetString(nameof(Arguments));
            ExitCode = info.GetInt32(nameof(ExitCode));
            ProcessOutput = info.GetValue(nameof(ProcessOutput), typeof(ProcessOutput)) as ProcessOutput;
        }

        public string Path { get; private set; }

        public string Arguments { get; private set; }

        public int ExitCode { get; private set; }

        public ProcessOutput ProcessOutput { get; private set; }

        public override string Message
        {
            get
            {
                var builder = new StringBuilder();
                if (!string.IsNullOrWhiteSpace(base.Message))
                {
                    builder.AppendLine(base.Message);
                }

                builder.AppendLine($"Process \"{Path} {Arguments}\" exited with code {ExitCode} ({ExitCode:X}h).");
                builder.AppendLine();
                if (ProcessOutput.ErrorLines.Any())
                {
                    builder.AppendLine("--- Error output ---");
                    foreach (var line in ProcessOutput.ErrorLines)
                    {
                        builder.AppendLine(line);
                    }
                }

                if (ProcessOutput.ErrorLines.Any())
                {
                    builder.AppendLine("--- Standard output ---");
                    foreach (var line in ProcessOutput.OutputLines)
                    {
                        builder.AppendLine(line);
                    }
                }

                return builder.ToString();
            }
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            info.AddValue(nameof(Path), Path);
            info.AddValue(nameof(Arguments), Arguments);
            info.AddValue(nameof(ExitCode), ExitCode);
            info.AddValue(nameof(ProcessOutput), ProcessOutput, typeof(ProcessOutput));
        }
    }
}