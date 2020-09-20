using System.CommandLine;
using System.IO;
using System.Threading;

namespace dnf_iis
{
    class CommandArguments
    {
        public IConsole Console { get; set; } = default!;

        public CancellationToken Token { get; set; }

        public DirectoryInfo Path { get; set; } = default!;

        public DirectoryInfo? SolutionPath { get; set; }

        public bool NoBuild { get; set; }

        public string? Name { get; set; }

        public int? Port { get; set; }
    }
}