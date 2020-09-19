using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Common
{
    public class BuildResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public string? Directory { get; set; }
        public string? File { get; set; }
    }

    public class MSBuild
    {
        #region Discovery 

        private static Lazy<Task<string[]>> _discoveryTask = new Lazy<Task<string[]>>(() => DiscoverMSBuild(), LazyThreadSafetyMode.ExecutionAndPublication);

        private static readonly Func<Task<string[]?>>[] DiscoveryPaths = new Func<Task<string[]?>>[]
        {
            DiscoverUsingVSWhere,
            DiscoverUsingDotnetCLI,
            DiscoverUsingRegistry
        };

        private static async Task<string[]> DiscoverMSBuild()
        {
            foreach (var discoverPath in DiscoveryPaths)
            {
                var msBuild = await discoverPath();
                if (msBuild != null)
                    return msBuild;
            }

            throw new Exception("Could not detect an MSBuild in the system.");
        }

        private static readonly string[] VSWherePaths = new[]
        {
            @"C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe",
            @"C:\Program Files\Microsoft Visual Studio\Installer\vswhere.exe"
        };

        private static async Task<string[]?> DiscoverUsingVSWhere()
        {
            static async Task<string?> GetCorrectPath()
            {
                foreach (var path in VSWherePaths)
                {
                    if (await Probe(path, "/h"))
                        return path;
                }

                return null;
            }

            var path = await GetCorrectPath();
            if (path == null)
                return null;

            var vsWhereArgs = @"-latest -products * -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe";

            foreach (var arg in new[] {vsWhereArgs, $"-prerelease {vsWhereArgs}"})
            {
                var res = await ProcessUtil.RunAsync(path, arg, throwOnError: false);
                if (res.ExitCode != 0)
                    continue;

                var msBuild = res.StandardOutput?.Trim();
                if (string.IsNullOrEmpty(msBuild))
                    continue;

                return new[] { msBuild };
            }

            return null;
        }

        private static async Task<string[]?> DiscoverUsingDotnetCLI()
        {
            if (await Probe("dotnet", "msbuild /h"))
                return new[] { "dotnet", "msbuild" };

            return null;
        }

        private static Task<string[]?> DiscoverUsingRegistry()
        {
            var subKey = Registry.LocalMachine
                .OpenSubKey(@"SOFTWARE\Microsoft\MSBuild\ToolsVersions\4.0");

            if (subKey == null)
                return Task.FromResult<string[]?>(null);

            var path = subKey!.GetValue("MSBuildToolsPath");
            if (path == null)
                return Task.FromResult<string[]?>(null);

            return Task.FromResult(new[] { Path.Join(path!.ToString(), "MSBuild.exe") })!;
        }

        private static async Task<bool> Probe(string path, string arg)
        {
            try
            {
                return (await ProcessUtil.RunAsync(path, arg, throwOnError: false)).ExitCode == 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion

        public async Task<BuildResult> BuildAndGetArtifactPath(string projectPath, string? solutionPath)
        {
            var msBuildPath = await _discoveryTask.Value;

            var buildRes = await ProcessUtil.RunAsync(
                msBuildPath[0],
                (msBuildPath.Length > 1 ? string.Join(" ", msBuildPath[1..]) + " " : "") + "-consoleLoggerParameters:Verbosity=minimal -nologo -restore:true" + (string.IsNullOrEmpty(solutionPath) ? "" : " -p:SolutionDir=" + solutionPath),
                projectPath,
                throwOnError: false);

            if (buildRes.ExitCode != 0)
            {
                return new BuildResult
                {
                    Success = false,
                    Error = buildRes.StandardOutput
                };
            }

            var regex = new Regex(@"\s*([^\s]*)\s*\-\>\s*(.*)");

            var artifactCopyLine = buildRes.StandardOutput.Split(Environment.NewLine)
                .Select(line => line?.Trim())
                .Where(line => !string.IsNullOrEmpty(line))
                .Where(line => regex.IsMatch(line))
                .LastOrDefault();

            if (artifactCopyLine == null)
            {
                return new BuildResult
                {
                    Success = false
                };
            }

            var artifactPath = regex.Match(artifactCopyLine).Groups.Values.Last().Value;

            return new BuildResult
            {
                Success = true,
                Directory = Path.GetDirectoryName(artifactPath),
                File = Path.GetFileName(artifactPath)
            };
        }
    }
}