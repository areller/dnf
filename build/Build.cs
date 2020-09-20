using System;
using System.Linq;
using System.Linq.Expressions;
using Nuke.Common;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.Tools.Git.GitTasks;

[CheckBuildProjectConfigurations]
[UnsetVisualStudioEnvironmentVariables]
[GitHubActions("ci", GitHubActionsImage.WindowsLatest, OnPushBranches = new[] { "master" }, OnPullRequestBranches = new[] { "master" }, OnPushTags = new[] { "v*" }, ImportSecrets = new[] { "NugetApiKey" })]
class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main() => Execute<Build>(x => x.All);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Solution] readonly Solution Solution;
    [GitRepository] readonly GitRepository GitRepository;

    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";
    AbsolutePath NugetDirectory => ArtifactsDirectory / "nuget";

    [Parameter] string NugetApiUrl = "https://api.nuget.org/v3/index.json";
    [Parameter] string NugetApiKey;

    Expression<Func<bool>> WhenMaster => () => GitRepository.Branch == "refs/heads/master";
    Expression<Func<bool>> WhenTagged => () => GitRepository.Branch.StartsWith("refs/tags/");
    Expression<Func<bool>> WhenNotTagged => () => !GitRepository.Branch.StartsWith("refs/tags/");

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            EnsureCleanDirectory(ArtifactsDirectory);
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(Solution));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .EnableNoRestore());
        });

    Target Test => _ => _
        .OnlyWhenDynamic(WhenNotTagged)
        .DependsOn(Compile)
        .Executes(() =>
        {
            foreach (var testProject in Solution.GetProjects("*.Tests"))
            {
                Logger.Info($"======== Running Tests for {testProject.Name} ========");
                DotNetTest(s => s
                    .SetProjectFile(testProject)
                    .SetConfiguration(Configuration)
                    .EnableNoRestore()
                    .EnableNoBuild());
            }
        });

    Target Pack => _ => _
        .DependsOn(Test)
        .Executes(() =>
        {
            foreach (var project in new[] { Solution.GetProject("dnf"), Solution.GetProject("dnf-iis") })
            {
                DotNetPack(s => s
                    .SetProject(project)
                    .SetConfiguration(Configuration)
                    .SetVersion(GetVersion())
                    .EnableNoRestore()
                    .EnableNoBuild()
                    .SetOutputDirectory(NugetDirectory));
            }
        });

    Target Publish => _ => _
        .OnlyWhenDynamic(WhenTagged)
        .OnlyWhenDynamic(() => Configuration.Equals(Configuration.Release))
        .DependsOn(Pack)
        .Requires(() => NugetApiUrl)
        .Requires(() => NugetApiKey)
        .Executes(() =>
        {
            GlobFiles(NugetDirectory, "*.nupkg")
                .NotEmpty()
                .Where(x => !x.EndsWith("symbols.nupkg"))
                .ForEach(x =>
                {
                    DotNetNuGetPush(s => s
                        .SetTargetPath(x)
                        .SetSource(NugetApiUrl)
                        .SetApiKey(NugetApiKey));
                });
        });

    Target All => _ => _
        .DependsOn(Publish)
        .DependsOn(Test);

    string GetVersion()
    {
        var tag = Git("tag --points-at HEAD");
        if (tag == null || tag.Count == 0)
            return null;

        return tag.First().Text.TrimStart('v').Trim();
    }
}