using System;
using System.Collections.Concurrent;
using System.Threading;

namespace ExpertFunicular.Server
{
    public class FunicularConnectionFactory
    {
        private static FunicularConnectionFactory _factory;
        private static readonly object Sync = new();
        
        private readonly IServiceProvider _serviceProvider;
        private readonly ConcurrentDictionary<string, IFunicularServer> _servers;

        private FunicularConnectionFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _servers = new ConcurrentDictionary<string, IFunicularServer>();
        }

        public static FunicularConnectionFactory New(IServiceProvider serviceProvider)
        {
            lock (Sync) _factory ??= new FunicularConnectionFactory(serviceProvider);
            return _factory;
        }

        public IFunicularConnection CreateServer(string pipeName, CancellationToken cancellationToken = default)
        {
            lock (Sync)
            {
                if (_servers.TryGetValue(pipeName, out var existingServer) && !existingServer.IsTerminated)
                    return new FunicularConnection(existingServer, _serviceProvider);
                
                existingServer = new FunicularServer(pipeName);
                _servers.AddOrUpdate(pipeName, _ => existingServer, (_, __) => existingServer);
                return new FunicularConnection(existingServer, _serviceProvider);
            }
        }
    }
}