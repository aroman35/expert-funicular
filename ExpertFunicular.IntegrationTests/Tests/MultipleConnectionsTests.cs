using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExpertFunicular.Client;
using ExpertFunicular.IntegrationTests.Mocks;
using ExpertFunicular.Server;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace ExpertFunicular.IntegrationTests.Tests
{
    public class MultipleConnectionsTests : IClassFixture<MultipleConnectionsTests.ConnectionsFixture>
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly ConnectionsFixture _connectionFactory;

        public MultipleConnectionsTests(ConnectionsFixture connectionsFactory, ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            _connectionFactory = connectionsFactory;
        }

        [Fact(DisplayName = "Access the same controller method from different pipes one by one")]
        public void AccessMultiplePipesOneByOne()
        {
            var request = new TestRequest
            {
                StringField = "Test request"
            };

            var stopwatch = Stopwatch.StartNew();
            foreach (var (pipeName, client) in _connectionFactory.Clients)
            {
                var response = client.Send<TestRequest, TestResponse>("test/receive-and-return-async", request);
                response.StringField.ShouldBe($"Received: {request.StringField} from pipe {pipeName}");
                _testOutputHelper.WriteLine(response.StringField);
            }
            
            stopwatch.Stop();
            stopwatch.Elapsed.ShouldBeGreaterThan(TimeSpan.FromSeconds(_connectionFactory.Clients.Count));
        }
        
        [Fact(DisplayName = "Access the same controller method from different pipes in parallel")]
        public void AccessMultiplePipesInParallel()
        {
            var request = new TestRequest
            {
                StringField = "Test request"
            };

            Parallel.ForEach(_connectionFactory.Clients, clientAndPipe =>
            {
                var (pipeName, client) = clientAndPipe;
                var response = client.Send<TestRequest, TestResponse>("test/receive-and-return-async", request);
                response.StringField.ShouldBe($"Received: {request.StringField} from pipe {pipeName}");
                _testOutputHelper.WriteLine(response.StringField);
            });
        }
        
        [Theory(DisplayName = "Access controller from the same client multiple times one by one")]
        [InlineData(1)]
        [InlineData(2)]
        public void AccessOneMethodFromTheSameClientMultipleTimes(int repeatTimes)
        {
            var request = new TestRequest
            {
                StringField = "Test request"
            };
            for (var i = 0; i < repeatTimes; i++)
            {
                var (pipeName, client) = _connectionFactory.Clients.First();
                var response = client.Send<TestRequest, TestResponse>("test/receive-and-return-async", request, 2000);
                response.StringField.ShouldBe($"Received: {request.StringField} from pipe {pipeName}");
                _testOutputHelper.WriteLine(response.StringField);
            }
        }

        public class ConnectionsFixture : IDisposable
        {
            public  IDictionary<string, IFunicularClient> Clients { get; }

            public ConnectionsFixture()
            {
                var connectionFactory = FunicularConnectionFactory.New(ConfigureServices());
                var funicularClientFactory = FunicularClientFactory.New();
                
                Clients = new Dictionary<string, IFunicularClient>();
                foreach (var pipeName in Enumerable.Range(0, 10).Select(x => $"test-pipe-{x}"))
                {
                    var server = connectionFactory.CreateServer(pipeName);
                    server.StartListening(CancellationToken.None);
                    Clients.Add(pipeName, funicularClientFactory.CreateClient(pipeName));
                }
            }

            private IServiceProvider ConfigureServices()
            {
                var services = new ServiceCollection();
                foreach (var controller in AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(s => s.GetTypes())
                    .Where(p => typeof(FunicularController).IsAssignableFrom(p) && !p.IsAbstract))
                {
                    services
                        .AddScoped(controller);
                }

                return services.BuildServiceProvider();
            }
            
            public void Dispose()
            {
                Console.WriteLine("Disposed!!!!");
            }
        }
    }
}