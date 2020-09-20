using Common;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace dnf_iis
{
    class IISHost : Host<CommandArguments>
    {
        private string? _envAppName;
        private int? _envPort;

        private MSBuild _msBuild;

        public IISHost(IDictionary<string, string> environmentVariables)
        {
            ExtractTyeEnvironmentVariables(environmentVariables);
            _msBuild = new MSBuild();
        }

        public override async Task Run(IConsole console, CommandArguments arguments, CancellationToken token)
        {
            var siteName = arguments.Name ?? _envAppName;
            var sitePort = arguments.Port ?? _envPort;

            if (string.IsNullOrEmpty(siteName))
                throw new Exception("Argument 'name' is required.");

            if (!sitePort.HasValue)
                throw new Exception("Argument 'port' is required.");

            var iisExpressPath = await IISExpressDiscovery.GetIISExpressPath();

            var siteConfig = new SiteConfig(siteName, arguments.Path.FullName, sitePort.Value);
            var configPath = await siteConfig.Create(TempDirectory.Value);

            if (!arguments.NoBuild)
            {
                var buildRes = await _msBuild.BuildAndGetArtifactPath(arguments.Path.FullName, arguments.SolutionPath?.FullName);
                if (!buildRes.Success)
                {
                    if (!string.IsNullOrEmpty(buildRes.Error))
                        throw new Exception(buildRes.Error);

                    throw new Exception("Failed to build website");
                }
            }

            var spec = new ProcessSpec
            {
                Executable = iisExpressPath,
                Arguments = $"/config:{configPath} /site:{siteName}",
                WorkingDirectory = arguments.Path.FullName,
                OutputData = data => console.Out.WriteLine(data),
                ErrorData = data => console.Error.WriteLine(data),
                OnStart = pid => console.Out.WriteLine($"IIS Express Started at PID {pid}"),
                OnStop = code => console.Out.WriteLine($"IIS Express Stopped with status code {code}"),
                CreateWindow = false
            };

            await ProcessUtil.RunAsync(spec, token, throwOnError: false);
        }

        private void ExtractTyeEnvironmentVariables(IDictionary<string, string> vars)
        {
            // See what environment variables Tye injects to processes
            // https://github.com/dotnet/tye/blob/3402fbddeea6a31310c181b48a6281f84865aabc/src/Microsoft.Tye.Hosting/ProcessRunner.cs#L273
            // https://github.com/dotnet/tye/blob/3402fbddeea6a31310c181b48a6281f84865aabc/src/Microsoft.Tye.Hosting/ProcessRunner.cs#L241

            if (vars.TryGetValue("APP_INSTANCE", out var appInstance))
            {
                var lastSeparator = appInstance.LastIndexOf('_');
                if (lastSeparator != -1)
                    _envAppName = appInstance.Substring(0, lastSeparator);
            }

            if (vars.TryGetValue("PORT", out var ports))
            {
                var firstPort = ports.Split(';').First();
                if (int.TryParse(firstPort, out var portNum))
                    _envPort = portNum;
            }
        }
    }
}