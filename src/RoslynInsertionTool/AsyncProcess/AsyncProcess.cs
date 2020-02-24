// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Threading.Tasks
{
    public static class AsyncProcess
    {
        public static Task<ProcessOutput> StartAsync(
            string executable,
            string arguments,
            bool lowPriority,
            CancellationToken cancellationToken,
            string workingDirectory = null,
            bool captureOutput = false,
            bool displayWindow = true,
            bool elevated = false,
            Predicate<int> isErrorCodeOk = null,
            Action<string> onOutputDataReceived = null,
            Action<string> onErrorDataReceived = null)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var taskCompletionSource = new TaskCompletionSource<ProcessOutput>();

            var process = new Process
            {
                EnableRaisingEvents = true
            };

            if (elevated)
            {
                process.StartInfo = CreateElevatedStartInfo(executable, arguments, workingDirectory);
            }
            else
            {
                process.StartInfo = CreateProcessStartInfo(executable, arguments, workingDirectory, captureOutput, displayWindow);
            }

            var task = CreateTaskAsync(process, taskCompletionSource, cancellationToken, isErrorCodeOk, onOutputDataReceived, onErrorDataReceived);

            process.Start();

            if (lowPriority)
            {
                process.PriorityClass = ProcessPriorityClass.BelowNormal;
            }

            if (process.StartInfo.RedirectStandardOutput)
            {
                process.BeginOutputReadLine();
            }

            if (process.StartInfo.RedirectStandardError)
            {
                process.BeginErrorReadLine();
            }

            return task;
        }

        private static Task<ProcessOutput> CreateTaskAsync(
            Process process,
            TaskCompletionSource<ProcessOutput> taskCompletionSource,
            CancellationToken cancellationToken,
            Predicate<int> isErrorCodeOk = null,
            Action<string> onOutputDataReceived = null,
            Action<string> onErrorDataReceived = null)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (taskCompletionSource == null)
            {
                throw new ArgumentNullException(nameof(taskCompletionSource));
            }

            if (process == null)
            {
                return taskCompletionSource.Task;
            }

            if (isErrorCodeOk == null)
            {
                isErrorCodeOk = exitCode => exitCode == 0;
            }

            var errorLines = new List<string>();
            var outputLines = new List<string>();

            process.OutputDataReceived += (s, e) =>
            {
                if (e.Data != null)
                {
                    outputLines.Add(e.Data);
                    onOutputDataReceived?.Invoke(e.Data);
                }
            };

            process.ErrorDataReceived += (s, e) =>
            {
                if (e.Data != null)
                {
                    errorLines.Add(e.Data);
                    onErrorDataReceived?.Invoke(e.Data);
                }
            };

            process.Exited += (s, e) =>
            {
                var processOutput = new ProcessOutput(process.ExitCode, outputLines, errorLines);
                if (isErrorCodeOk?.Invoke(process.ExitCode) == true)
                {
                    taskCompletionSource.TrySetResult(processOutput);
                }
                else
                {
                    taskCompletionSource.TrySetException(new ProcessFailureException(process.StartInfo.FileName, process.StartInfo.Arguments, process.ExitCode, processOutput));
                }
            };

            var registration = cancellationToken.Register(() =>
            {
                if (taskCompletionSource.TrySetCanceled())
                {
                    // If the underlying process is still running, we should kill it
                    if (!process.HasExited)
                    {
                        try
                        {
                            process.Kill();
                        }
                        catch (InvalidOperationException)
                        {
                            // Ignore, since the process is already dead
                        }
                    }
                }
            });

            return taskCompletionSource.Task;
        }

        private static ProcessStartInfo CreateProcessStartInfo(
            string executable,
            string arguments,
            string workingDirectory,
            bool captureOutput,
            bool displayWindow,
            Dictionary<string, string> environmentVariables = null)
        {
            var processStartInfo = new ProcessStartInfo(executable, arguments);

            if (!string.IsNullOrEmpty(workingDirectory))
            {
                processStartInfo.WorkingDirectory = workingDirectory;
            }

            if (environmentVariables != null)
            {
                foreach (var pair in environmentVariables)
                {
                    processStartInfo.EnvironmentVariables[pair.Key] = pair.Value;
                }
            }

            if (captureOutput)
            {
                processStartInfo.UseShellExecute = false;
                processStartInfo.RedirectStandardOutput = true;
                processStartInfo.RedirectStandardError = true;
            }
            else
            {
                processStartInfo.CreateNoWindow = !displayWindow;
                processStartInfo.UseShellExecute = displayWindow;
            }

            return processStartInfo;
        }

        private static ProcessStartInfo CreateElevatedStartInfo(string executable, string arguments, string workingDirectory)
        {
            var adminInfo = new ProcessStartInfo(executable, arguments)
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                Verb = "runas"
            };

            if (!string.IsNullOrEmpty(workingDirectory))
            {
                adminInfo.WorkingDirectory = workingDirectory;
            }

            return adminInfo;
        }
    }
}
