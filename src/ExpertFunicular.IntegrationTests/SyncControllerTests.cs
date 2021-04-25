using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExpertFunicular.Client;
using ExpertFunicular.Server;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace ExpertFunicular.IntegrationTests
{
    public class SyncControllerTests
    {
        private readonly IServiceProvider _serviceProvider;
        private const string PipeName = "test-pipe";

        public SyncControllerTests()
        {
            var services = new ServiceCollection();
            AddPipeControllers(services);
            _serviceProvider = services.BuildServiceProvider();
        }

        [Fact]
        public void LaunchServerAndHandleRequestResponseSync()
        {
            using var server = FunicularConnectionFactory.New(_serviceProvider).CreateServer("test-pipe");
            var client = FunicularClientFactory.New().CreateClient("test-pipe");
            var request = new TestRequest
            {
                StringField = "requestedFiled"
            };

            var cancellationTokenSource = new CancellationTokenSource();
            server.StartListening(cancellationTokenSource.Token);
            var response = client.Send<TestRequest, TestResponse>("test/receive-and-return", request);
            
            response.StringField.ShouldBe($"Received: {request.StringField}");
            cancellationTokenSource.Cancel();
        }
        
        [Fact]
        public void LaunchServerAndHandleRequestResponseAsync()
        {
            using var server = FunicularConnectionFactory.New(_serviceProvider).CreateServer("test-pipe-2");
            var client = FunicularClientFactory.New().CreateClient("test-pipe-2");
            var request = new TestRequest
            {
                StringField = "requestedFiled"
            };

            var cancellationTokenSource = new CancellationTokenSource();
            server.StartListening(cancellationTokenSource.Token);
            var stopwatch = Stopwatch.StartNew();
            var response = client.Send<TestRequest, TestResponse>("test/receive-and-return-async", request);
            stopwatch.Stop();
            
            // delay in controller
            stopwatch.Elapsed.ShouldBeGreaterThan(TimeSpan.FromMilliseconds(1000));
            
            response.StringField.ShouldBe($"Received: {request.StringField}");
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
        
        [PipeRoute("test")]
        public class TestController : FunicularController
        {
            [PipeRoute("receive-and-return")]
            public TestResponse ReceiveAndResponseAction(TestRequest request)
            {
                var response = new TestResponse();
                response.StringField = $"Received: {request.StringField}";

                return response;
            }
            
            [PipeRoute("receive-and-return-async")]
            public async Task<TestResponse> ReceiveAndResponseActionAsync(TestRequest request)
            {
                var response = new TestResponse();
                response.StringField = $"Received: {request.StringField}";
                await Task.Delay(1000);

                return response;
            }
        }
        
        public class TestRequest
        {
            public string StringField { get; set; }
        }
        
        public class TestResponse
        {
            public string StringField { get; set; }
        }
    }
}