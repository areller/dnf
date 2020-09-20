using Common;
using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.IO;
using System.Threading.Tasks;

namespace dnf_iis
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var command = new RootCommand("Run a .NET Framework website in IIS")
            {
                new Argument<DirectoryInfo?>(CLIHelper.ParsePath<DirectoryInfo>)
                {
                    Arity = ArgumentArity.ExactlyOne,
                    Description = "The relative or absolute path to the .NET Framework project directory",
                    Name = "path",
                },
                new Argument<DirectoryInfo?>(CLIHelper.ParsePath<DirectoryInfo>)
                {
                    Arity = ArgumentArity.ZeroOrOne,
                    Description = "The relative or absolute path to the solution directory",
                    Name = "solutionPath"
                },
                new Option("--no-build", "Do not build the website before launching it"),
                new Option("--name", "The name of the website")
                {
                    Argument = new Argument<string>("name")
                },
                new Option("--port", "The port that the website should listen on")
                {
                    Argument = new Argument<int>("port")
                }
            };

            command.Handler = CommandHandler.Create<CommandArguments>(async arg =>
            {
                if (string.IsNullOrEmpty(arg.Path?.FullName))
                    throw new Exception("Argument 'path' is required.");

                var environmentVariables = EnvironmentHelper.LoadEnvironmentVariables();

                await using var iisHost = new IISHost(environmentVariables);
                await iisHost.Run(arg.Console, arg, arg.Token);
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