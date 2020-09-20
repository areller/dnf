using Common;
using System;
using System.CommandLine;
using System.CommandLine.IO;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace dnf
{
    class DNFHost : Host<CommandArguments>
    {
        public DNFHost()
        {
        }

        public override async Task Run(IConsole console, CommandArguments arguments, CancellationToken token)
        {
            var watcher = new ArtifactsWatcher(console);
            var msBuild = new MSBuild(console);

            var buildRes = await msBuild.BuildAndGetArtifactPath(arguments.Path.FullName, arguments.SolutionPath?.FullName);
            if (!buildRes.Success)
            {
                if (!string.IsNullOrEmpty(buildRes.Error))
                    throw new Exception(buildRes.Error);

                throw new Exception("Could not determine path of built artifacts");
            }

            while (!token.IsCancellationRequested)
            {
                await using var buildDirectory = new TemporaryDirectory(TempDirectory.Value);

                console.Out.WriteLine($"Copying artifacts from '{buildRes.Directory}' to '{buildDirectory.Value}'");
                buildDirectory.CopyFrom(buildRes.Directory!);

                using var cts = new CancellationTokenSource();
                await using var _ = token.Register(() => cts.Cancel());

                var watch = !arguments.NoRestart ? watcher.WatchUntilRebuild(buildRes.Directory!, buildRes.File!, cts.Token) : Task.Delay(-1, cts.Token);
                var process = StartProcess(console, arguments, buildDirectory.Value, buildRes.File!, cts.Token);

                var firstTask = await Task.WhenAny(watch, process);

                cts.Cancel();

                if (firstTask == process)
                    break;
                else if (!token.IsCancellationRequested)
                    console.Out.WriteLine($"Restarting {buildRes.File!} due to rebuild");

                await process;
            }
        }

        private Task<ProcessResult> StartProcess(IConsole console, CommandArguments arguments, string dir, string file, CancellationToken token)
        {
            var spec = new ProcessSpec
            {
                Executable = Path.Join(dir, file),
                Arguments = arguments.Arguments,
                WorkingDirectory = dir,
                OutputData = data => console.Out.WriteLine(data),
                ErrorData = data => console.Error.WriteLine(data),
                OnStart = pid => console.Out.WriteLine($"{file} Started at PID {pid}"),
                OnStop = code => console.Out.WriteLine($"{file} Stopped with status code {code}")
            };

            return ProcessUtil.RunAsync(spec, token, throwOnError: false);
        }
    }
}