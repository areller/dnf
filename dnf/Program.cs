using Common;
using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.IO;
using System.Threading.Tasks;

namespace dnf
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var command = new RootCommand("Run a .NET Framework project")
            {
                new Argument<DirectoryInfo?>(CLIHelper.ParsePath<DirectoryInfo>)
                {
                    Arity = ArgumentArity.ExactlyOne,
                    Description = "The relative or absolute path to the .NET Framework project directory",
                    Name = "path"
                },
                new Argument<DirectoryInfo?>(CLIHelper.ParsePath<DirectoryInfo>)
                {
                    Arity = ArgumentArity.ZeroOrOne,
                    Description = "The relative or absolute path to the solution directory",
                    Name = "solutionPath"
                },
                new Option<bool>("--no-restart", "Keep running project even after rebuilds")
                {
                    IsRequired = false
                }
            };

            command.Handler = CommandHandler.Create<CommandArguments>(async arg =>
            {
                if (string.IsNullOrEmpty(arg.Path?.FullName))
                    throw new Exception("Argument 'path' is required.");

                arg.Arguments = CLIHelper.ParseArguments();

                await using var dnfHost = new DNFHost();
                await dnfHost.Run(arg.Console, arg, arg.Token);
            });

            var builder = new CommandLineBuilder(command);
            builder.UseHelp();
            builder.UseDebugDirective();
            builder.UseExceptionHandler(CLIHelper.HandleException);
            builder.CancelOnProcessTermination();

            var parser = builder.Build();
            await parser.InvokeAsync(args);
        }
    }
}
