using Common;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TestsHelpers;
using Xunit;
using Xunit.Abstractions;
using static TestsHelpers.Helpers;

namespace dnf_iis.Tests
{
    public class IISHostTests
    {
        private readonly IConsole _console;

        public IISHostTests(ITestOutputHelper output)
        {
            _console = new TestsConsole(output);
        }

        [Fact]
        public async Task ShouldNotWorkIfNotBuilt()
        {
            await using var testSolution = await CopyTestAssets("demowebsite");
            var projectPath = Path.Join(testSolution.Value, "demowebsite");

            await using var iisHost = new IISHost(new Dictionary<string, string>());
            using var cancel = new CancellationTokenSource();

            var port = CreateRandomPort();
            var name = "demosolutionsitename";
            var run = iisHost.Run(_console, new CommandArguments
            {
                Path = new DirectoryInfo(projectPath),
                Port = port,
                Name = name,
                NoBuild = true
            }, cancel.Token);

            try
            {
                var httpClient = new HttpClient();
                var res = await Retry.Get(() => httpClient.GetAsync($"http://localhost:{port}/DemoApi"), 10, Retry.ConstantTimeBackOff());

                Assert.False(res.IsSuccessStatusCode);
            }
            finally
            {
                cancel.Cancel();
                await run;
            }
        }

        [Fact]
        public async Task ShouldRunWebsite()
        {
            await using var testSolution = await CopyTestAssets("demowebsite");
            var projectPath = Path.Join(testSolution.Value, "demowebsite");

            await using var iisHost = new IISHost(new Dictionary<string, string>());
            using var cancel = new CancellationTokenSource();

            var port = CreateRandomPort();
            var name = "demosolutionsitename";
            var run = iisHost.Run(_console, new CommandArguments
            {
                Path = new DirectoryInfo(projectPath),
                Port = port,
                Name = name
            }, cancel.Token);

            try
            {
                var httpClient = new HttpClient();
                var res = await Retry.Get(() => httpClient.GetAsync($"http://localhost:{port}/DemoApi"), 10, Retry.ConstantTimeBackOff());
                Assert.True(res.IsSuccessStatusCode);

                var json = await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(await res.Content.ReadAsStreamAsync());

                Assert.Equal(name, json["siteName"]);
            }
            finally
            {
                cancel.Cancel();
                await run;
            }
        }
    }
}