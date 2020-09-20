using System.CommandLine;
using System.IO;
using System.Threading;

namespace dnf
{
    class CommandArguments
    {
        public IConsole Console { get; set; } = default!;

        public CancellationToken Token { get; set; } = default!;

        public DirectoryInfo Path { get; set; } = default!;

        public DirectoryInfo? SolutionPath { get; set; }

        public string Arguments { get; set; } = default!;

        public bool NoRestart { get; set; }
    }
}