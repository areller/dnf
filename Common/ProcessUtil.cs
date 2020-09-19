// Copied from Project Tye (https://github.com/dotnet/tye)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Common
{
    public class ProcessSpec
    {
        public string? Executable { get; set; }
        public string? WorkingDirectory { get; set; }
        public IDictionary<string, string> EnvironmentVariables { get; set; } = new Dictionary<string, string>();
        public string? Arguments { get; set; }
        public Action<string>? OutputData { get; set; }
        public Func<Task<int>>? Build { get; set; }
        public Action<string>? ErrorData { get; set; }
        public Action<int>? OnStart { get; set; }
        public Action<int>? OnStop { get; set; }
        public string? ShortDisplayName()
            => Path.GetFileNameWithoutExtension(Executable);
    }

    public class ProcessResult
    {
        public ProcessResult(string standardOutput, string standardError, int exitCode)
        {
            StandardOutput = standardOutput;
            StandardError = standardError;
            ExitCode = exitCode;
        }

        public string StandardOutput { get; }
        public string StandardError { get; }
        public int ExitCode { get; }
    }

    public static class ProcessUtil
    {
        #region Native Methods

        [DllImport("libc", SetLastError = true, EntryPoint = "kill")]
        private static extern int sys_kill(int pid, int sig);

        #endregion

        private static readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        public static async Task<ProcessResult> RunAsync(
            string filename,
            string arguments,
            string? workingDirectory = null,
            bool throwOnError = true,
            IDictionary<string, string>? environmentVariables = null,
            Action<string>? outputDataReceived = null,
            Action<string>? errorDataReceived = null,
            Action<int>? onStart = null,
            Action<int>? onStop = null,
            CancellationToken cancellationToken = default)
        {
            using var process = new Process()
            {
                StartInfo =
                {
                    FileName = filename,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = false,
                    UseShellExecute = false,
                    CreateNoWindow = !IsWindows,
                    WindowStyle = ProcessWindowStyle.Hidden
                },
                EnableRaisingEvents = true
            };

            if (workingDirectory != null)
            {
                process.StartInfo.WorkingDirectory = workingDirectory;
            }

            if (environmentVariables != null)
            {
                foreach (var kvp in environmentVariables)
                {
                    process.StartInfo.Environment.Add(kvp!);
                }
            }

            var outputBuilder = new StringBuilder();
            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    if (outputDataReceived != null)
                    {
                        outputDataReceived.Invoke(e.Data);
                    }
                    else
                    {
                        outputBuilder.AppendLine(e.Data);
                    }
                }
            };

            var errorBuilder = new StringBuilder();
            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    if (errorDataReceived != null)
                    {
                        errorDataReceived.Invoke(e.Data);
                    }
                    else
                    {
                        errorBuilder.AppendLine(e.Data);
                    }
                }
            };

            var processLifetimeTask = new TaskCompletionSource<ProcessResult>();

            process.Exited += (_, e) =>
            {
                if (throwOnError && process.ExitCode != 0)
                {
                    processLifetimeTask.TrySetException(new InvalidOperationException($"Command {filename} {arguments} returned exit code {process.ExitCode}"));
                }
                else
                {
                    processLifetimeTask.TrySetResult(new ProcessResult(outputBuilder.ToString(), errorBuilder.ToString(), process.ExitCode));
                }
            };

            process.Start();
            onStart?.Invoke(process.Id);

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            var cancelledTcs = new TaskCompletionSource<object?>();
            await using var _ = cancellationToken.Register(() => cancelledTcs.TrySetResult(null));

            var result = await Task.WhenAny(processLifetimeTask.Task, cancelledTcs.Task);

            if (result == cancelledTcs.Task)
            {
                if (!IsWindows)
                {
                    sys_kill(process.Id, sig: 2); // SIGINT
                }
                else
                {
                    if (!process.CloseMainWindow())
                    {
                        process.Kill();
                    }
                }

                if (!process.HasExited)
                {
                    var cancel = new CancellationTokenSource();
                    await Task.WhenAny(processLifetimeTask.Task, Task.Delay(TimeSpan.FromSeconds(5), cancel.Token));
                    cancel.Cancel();

                    if (!process.HasExited)
                    {
                        process.Kill();
                    }
                }
            }

            var processResult = await processLifetimeTask.Task;
            onStop?.Invoke(processResult.ExitCode);
            return processResult;
        }

        public static Task<ProcessResult> RunAsync(ProcessSpec processSpec, CancellationToken cancellationToken = default, bool throwOnError = true)
        {
            return RunAsync(processSpec.Executable!, processSpec.Arguments!, processSpec.WorkingDirectory, throwOnError: throwOnError, processSpec.EnvironmentVariables, processSpec.OutputData, processSpec.ErrorData, processSpec.OnStart, processSpec.OnStop, cancellationToken);
        }

        public static void KillProcess(int pid)
        {
            try
            {
                using var process = Process.GetProcessById(pid);
                process?.Kill();
            }
            catch (ArgumentException) { }
            catch (InvalidOperationException) { }
        }
    }
}