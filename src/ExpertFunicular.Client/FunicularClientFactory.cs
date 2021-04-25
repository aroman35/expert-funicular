using System;
using System.Collections.Concurrent;

namespace ExpertFunicular.Client
{
    public class FunicularClientFactory
    {
        private static FunicularClientFactory _factory;
        private static readonly object Sync = new();
        private readonly ConcurrentDictionary<string, IFunicularClient> _clients;

        public static FunicularClientFactory New()
        {
            lock (Sync) _factory ??= new FunicularClientFactory();
            return _factory;
        }
        
        private FunicularClientFactory()
        {
            _clients = new ConcurrentDictionary<string, IFunicularClient>();
        }
        
        public IFunicularClient CreateClient(string pipeName)
        {
            lock (Sync)
            {
                if (_clients.TryGetValue(pipeName, out var existingClient))
                    return existingClient;
                
                existingClient = new FunicularClient(new PipeClient(pipeName));
                _clients.TryAdd(pipeName, existingClient);
                return existingClient;
            }
        }

        public IFunicularClient CreateClient(string pipeName, Action<string, Exception> errorHandler)
        {
            var client = CreateClient(pipeName);
            client.SetErrorHandler(errorHandler);
            return client;
        }
    }
}