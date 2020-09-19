using System.IO;
using System.Threading.Tasks;
using Xunit;
using static TestsHelpers.Helpers;

namespace Common.Tests
{
    public class MSBuildTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ShouldBuildAndFindArtifact(bool provideSolutionPath)
        {
            await using var testSolution = await CopyTestAssets("democonsole");
            var projectPath = Path.Join(testSolution.Value, "democonsole");

            var msBuild = new MSBuild();
            var buildRes = await msBuild.BuildAndGetArtifactPath(projectPath, provideSolutionPath ? testSolution.Value : null);

            Assert.Equal(provideSolutionPath, buildRes.Success);

            if (buildRes.Success)
            {
                Assert.Equal($@"{projectPath}\bin\Debug", buildRes.Directory);
                Assert.Equal("democonsole.exe", buildRes.File);
            }
        }
    }
}