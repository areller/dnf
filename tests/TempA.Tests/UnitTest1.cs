using Common;
using System;
using System.CommandLine;
using System.CommandLine.IO;
using System.IO;
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
        }
    }
}
