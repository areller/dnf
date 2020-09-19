﻿using System.CommandLine;
using System.IO;
using System.Threading.Tasks;
using TestsHelpers;
using static TestsHelpers.Helpers;
using Xunit;
using Xunit.Abstractions;
using System.Threading;
using System;
using System.Linq;
using Common;
using System.Diagnostics;
using System.CommandLine.IO;

namespace dnf.Tests
{
    public class DNFHostTests
    {
        private readonly IConsole _console;

        public DNFHostTests(ITestOutputHelper output)
        {
            _console = new TestsConsole(output);
        }

        [Fact]
        public async Task ShouldRunHostAndRecordOutput()
        {
            await using var testSolution = await CopyTestAssets("democonsole");
            var projectPath = Path.Join(testSolution.Value, "democonsole");

            await using var dnfHost = new DNFHost();
            using var cancel = new CancellationTokenSource();

            var outputTcs = new TaskCompletionSource<int>();
            Action<bool, string> capture = (error, message) =>
            {
                if (!error && message == "Hello world")
                    outputTcs.TrySetResult(0);
            };

            var run = dnfHost.Run(new MultiplexerConsole(new[] { _console, new CaptureConsole(capture) }), new CommandArguments
            {
                Path = new DirectoryInfo(projectPath),
                SolutionPath = new DirectoryInfo(testSolution.Value)
            }, cancel.Token);

            await WaitOrTimeout(outputTcs.Task);

            cancel.Cancel();
            await run;
        }

        [Fact]
        public async Task ShouldExitHostWhenProcessExits()
        {
            await using var testSolution = await CopyTestAssets("democonsole");
            var projectPath = Path.Join(testSolution.Value, "democonsole");

            await using var dnfHost = new DNFHost();

            int pid = 0;
            Action<bool, string> capture = (error, message) =>
            {
                if (!error && message.Contains("Started at PID"))
                {
                    pid = int.Parse(message.Split(" ").Last());
                }
                else if (!error && message == "Hello world")
                {
                    Process.GetProcessById(pid).Kill();
                }
            };
            var run = dnfHost.Run(new MultiplexerConsole(new[] { _console, new CaptureConsole(capture) }), new CommandArguments
            {
                Path = new DirectoryInfo(projectPath),
                SolutionPath = new DirectoryInfo(testSolution.Value)
            }, default);

            await WaitOrTimeout(run);
        }

        [Fact]
        public async Task ShouldRestartUponRebuild()
        {
            await using var testSolution = await CopyTestAssets("democonsole");
            var projectPath = Path.Join(testSolution.Value, "democonsole");

            var msBuild = new MSBuild();
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
                SolutionPath = new DirectoryInfo(testSolution.Value)
            }, cancel.Token);

            await finishedBuild.Task;
            await WaitOrTimeout(newMessageArrived.Task);

            cancel.Cancel();
            await run;
        }
    }
}