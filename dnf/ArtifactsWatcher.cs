using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace dnf
{
    class ArtifactsWatcher
    {
        public async Task WatchUntilRebuild(string artifactsDir, string filename, CancellationToken token)
        {
            var tcs = new TaskCompletionSource<int>();

            await using var _ = token.Register(() => tcs.TrySetResult(0));

            using var watcher = new FileSystemWatcher(artifactsDir);
            watcher.NotifyFilter = NotifyFilters.LastWrite;

            FileSystemEventHandler callback = (_, e) =>
            {
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