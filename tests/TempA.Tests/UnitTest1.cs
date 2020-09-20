using Common;
using dnf;
using System;
using System.CommandLine;
using System.CommandLine.IO;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TestsHelpers;
using Xunit;
using Xunit.Abstractions;
using static TestsHelpers.Helpers;

namespace TempA.Tests
{
    public class UnitTest1
    {
        private readonly IConsole _console;

        public UnitTest1(ITestOutputHelper output)
        {
            _console = new TestsConsole(output);
        }

        /*
        [Fact]
        public async Task Test1()
        {
            await using var dir = new TemporaryDirectory();
            File.WriteAllText(Path.Join(dir.Value, "sometext.txt"), "Message A");

            using var watcher = new FileSystemWatcher(dir.Value);
            watcher.NotifyFilter = NotifyFilters.LastWrite;

            var tcs = new TaskCompletionSource<int>();

            FileSystemEventHandler callback = (_, e) =>
            {
                _console.Out.WriteLine("File changed: " + e.Name);
                tcs.TrySetResult(0);
            };

            watcher.Changed += callback;
            watcher.EnableRaisingEvents = true;

            File.WriteAllText(Path.Join(dir.Value, "sometext.txt"), "Message B");
            await WaitOrTimeout(tcs.Task);

            watcher.EnableRaisingEvents = false;
            watcher.Changed -= callback;

            Assert.True(false);
        }*/

        [Theory]
        [InlineData(false)]
        public async Task ShouldRestartUponRebuild(bool noRestart)
        {
            await using var testSolution = await CopyTestAssets("democonsole");
            var projectPath = Path.Join(testSolution.Value, "democonsole");

            var msBuild = new MSBuild(_console);
            await using var dnfHost = new DNFHost();
            using var cancel = new CancellationTokenSource();

            var finishedBuild = new TaskCompletionSource<int>();
            var newMessageArrived = new TaskCompletionSource<int>();

            int originalPid = 0;
            Action<bool, string> capture = (error, message) =>
            {
                if (!error && message.Contains("Started at PID"))
                {
                    originalPid = int.Parse(message.Split(" ").Last());
                }
                else if (!error && message == "Message A")
                {
                    var projectCs = Path.Join(projectPath, "Program.cs");
                    File.WriteAllText(projectCs, File.ReadAllText(projectCs).Replace("Message A", "Message B"));
                    msBuild.BuildAndGetArtifactPath(projectPath, testSolution.Value)
                        .ContinueWith(res =>
                        {
                            if (!res.Result.Success)
                                _console.Error.WriteLine(res.Result.Error);

                            var filePath = Path.Join(projectPath, "bin", "Debug", "sometext.txt");
                            _console.Out.WriteLine("Writing to file path: " + filePath);
                            File.WriteAllText(filePath, "Message A");
                            File.WriteAllText(filePath, "Message C");

                            _console.Out.WriteLine(File.ReadAllText(filePath));

                            finishedBuild.TrySetResult(0);
                        });
                }
                else if (!error && message == "Message B")
                {
                    newMessageArrived.TrySetResult(0);
                }
            };
            var run = dnfHost.Run(new MultiplexerConsole(new[] { _console, new CaptureConsole(capture) }), new CommandArguments
            {
                Path = new DirectoryInfo(projectPath),
                SolutionPath = new DirectoryInfo(testSolution.Value),
                NoRestart = noRestart
            }, cancel.Token);

            try
            {
                Task.Run(async () =>
                {
                    var artifactWatcher = new ArtifactsWatcher(_console);
                    _console.Out.WriteLine("Starting second watch");
                    await artifactWatcher.WatchUntilRebuild(Path.Join(projectPath, "bin", "Debug"), "sometext.txt", default);
                    _console.Out.WriteLine("Finished watching");
                });

                await finishedBuild.Task;

                if (!noRestart)
                    await WaitOrTimeout(newMessageArrived.Task, 60000);
                else
                    await ShouldTimeout(newMessageArrived.Task);
            }
            finally
            {
                cancel.Cancel();
                await run;
            }
        }
    }
}
