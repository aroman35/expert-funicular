using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using ExpertFunicular.Client;
using ExpertFunicular.IntegrationTests.Mocks;
using ExpertFunicular.Server;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace ExpertFunicular.IntegrationTests.Tests
{
    public class SyncControllerTests
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly IServiceProvider _serviceProvider;

        public SyncControllerTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            var services = new ServiceCollection();
            AddPipeControllers(services);
            _serviceProvider = services.BuildServiceProvider();
        }

        [Fact(DisplayName = "Access a void-method in a controller and receive the result.")]
        public void LaunchServerAndHandleRequestResponseSync()
        {
            using var server = FunicularConnectionFactory.New(_serviceProvider).CreateServer("test-pipe");
            var client = FunicularClientFactory.New()
                .CreateClient("test-pipe", (pipe, error) => _testOutputHelper.WriteLine($"{pipe}: {error.Message}"));

            var request = new TestRequest
            {
                StringField = "requestedFiled"
            };

            var cancellationTokenSource = new CancellationTokenSource();
            server.StartListening(
                (error, pipe) => _testOutputHelper.WriteLine($"{pipe}: {error.Message}"),
                cancellationTokenSource.Token);
            var response = client.Send<TestRequest, TestResponse>("test/receive-and-return", request);

            response.StringField.ShouldBe($"Received: {request.StringField} from pipe test-pipe");
            cancellationTokenSource.Cancel();
        }

        [Fact(DisplayName = "Access an awaitable method in a controller and receive the result.")]
        public void LaunchServerAndHandleRequestResponseAsync()
        {
            using var server = FunicularConnectionFactory.New(_serviceProvider).CreateServer("0-test-pipe");
            var client = FunicularClientFactory.New()
                .CreateClient("0-test-pipe", (pipe, error) => _testOutputHelper.WriteLine($"{pipe}: {error.Message}"));

            var request = new TestRequest
            {
                StringField = "requestedFiled"
            };

            var cancellationTokenSource = new CancellationTokenSource();
            server.StartListening(
                (error, pipe) => _testOutputHelper.WriteLine($"{pipe}: {error.Message}"),
                cancellationTokenSource.Token);

            var stopwatch = Stopwatch.StartNew();
            var response = client.Send<TestRequest, TestResponse>("test/receive-and-return-async", request);
            stopwatch.Stop();

            // delay in controller
            stopwatch.Elapsed.ShouldBeGreaterThan(TimeSpan.FromMilliseconds(1000));

            response.StringField.ShouldBe($"Received: {request.StringField} from pipe 0-test-pipe");
            cancellationTokenSource.Cancel();
        }

        private void AddPipeControllers(IServiceCollection services)
        {
            foreach (var controller in AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => typeof(FunicularController).IsAssignableFrom(p) && !p.IsAbstract))
            {
                services
                    .AddScoped(controller);
            }
        }
    }
}