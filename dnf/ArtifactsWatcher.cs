using System.CommandLine;
using System.CommandLine.IO;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace dnf
{
    class ArtifactsWatcher
    {
        private IConsole _console;

        public ArtifactsWatcher(IConsole console)
        {
            _console = console;
        }

        public async Task WatchUntilRebuild(string artifactsDir, string filename, CancellationToken token)
        {
            var tcs = new TaskCompletionSource<int>();

            _console.Out.WriteLine("Watching directory " + artifactsDir);

            await using var _ = token.Register(() => tcs.TrySetResult(0));

            using var watcher = new FileSystemWatcher(artifactsDir);
            watcher.NotifyFilter = NotifyFilters.LastWrite;

            FileSystemEventHandler callback = (_, e) =>
            {
                _console.Out.WriteLine("File changed: " + e.Name);
                if (e.Name == filename && (e.ChangeType == WatcherChangeTypes.Changed || e.ChangeType == WatcherChangeTypes.Created))
                {
                    tcs.TrySetResult(0);
                }
            };

            watcher.Changed += callback;
            watcher.EnableRaisingEvents = true;

            await tcs.Task;

            watcher.EnableRaisingEvents = false;
            watcher.Changed -= callback;
        }
    }
}