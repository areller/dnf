using System;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;

namespace Common
{
    public abstract class Host<TArgs> : IAsyncDisposable
    {
        protected TemporaryDirectory TempDirectory { get; }

        protected Host()
        {
            TempDirectory = new TemporaryDirectory();
        }

        public abstract Task Run(IConsole console, TArgs arguments, CancellationToken token);

        public async ValueTask DisposeAsync()
        {
            await TempDirectory.DisposeAsync();
        }
    }
}