using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ExpertFunicular.Client;
using ExpertFunicular.Common.Exceptions;
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

        [Fact(DisplayName = "Post data")]
        public void SendTest()
        {
            var request = new TestRequest
            {
                StringField = "Test request"
            };
            var (pipeName, client) = _connectionFactory.Clients.First();
            client.Send("/test-2/receive", request);
            Thread.Sleep(2000); // receiving by controller is not awaited by client!
            TestController_2.RequestAccessor.StringField.ShouldBe(request.StringField);
        }
        
        [Theory(DisplayName = "Send large data")]
        [InlineData(1)]
        [InlineData(16)]
        [InlineData(64)]
        [InlineData(128)]
        [InlineData(256)]
        [InlineData(512)]
        public void SendLargePieceOfTextTest(int sizeInMegaBytes)
        {
            var stopwatch = Stopwatch.StartNew();
            var largeString = new string(Enumerable.Repeat('A', sizeInMegaBytes * 0x00100000).ToArray());
            var request = new TestRequest
            {
                StringField = largeString
            };
            var (pipeName, client) = _connectionFactory.Clients.First();
            var response = client.Send<TestRequest, TestResponse>("/test-2/receive-large-string", request);
            response.StringField.ShouldBe(largeString);
            stopwatch.Stop();
            _testOutputHelper.WriteLine($"MBytes: {sizeInMegaBytes} handled in {stopwatch.Elapsed:c}");
        }

        [Fact(DisplayName = "Get small piece of data")]
        public void GetSmallData()
        {
            var (pipeName, client) = _connectionFactory.Clients.First();
            var response = client.Get<TestResponse>("/test-2/get");
            Encoding.UTF8.GetBytes(response.StringField).Length.ShouldBe(0x00100000);
        }

        [Fact(DisplayName = "Get large piece of data")]
        public void GetLargeData()
        {
            var (pipeName, client) = _connectionFactory.Clients.First();
            var response = client.Get<TestResponse>("/test-2/get-huge");
            Encoding.UTF8.GetBytes(response.StringField).Length.ShouldBe(0x20000000);
        }
        
        [Fact(DisplayName = "Request an invalid controller method")]
        public void RequestInvalidMethod()
        {
            var (pipeName, client) = _connectionFactory.Clients.First();
            Should.Throw<FunicularPipeException>(() => client.Get<TestResponse>("/test-2/notfoundpath"));

        }
        
        [Fact(DisplayName = "Request an invalid url")]
        public void RequestInvalidUrl()
        {
            var (pipeName, client) = _connectionFactory.Clients.First();
            Should.Throw<FunicularPipeException>(() => client.Get<TestResponse>("/notfoundpath"));
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