using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ExpertFunicular.Common.Exceptions;
using ExpertFunicular.Common.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace ExpertFunicular.Server
{
    internal class FunicularConnection : IFunicularConnection
    {
        private readonly IDictionary<string, Type> _baseRoutePaths;
        private readonly IServiceProvider _serviceProvider;
        private readonly IFunicularServer _funicularServer;

        internal FunicularConnection(
            IFunicularServer funicularServer,
            IServiceProvider serviceProvider)
        {
            _funicularServer = funicularServer;
            _serviceProvider = serviceProvider;
            _baseRoutePaths = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => typeof(FunicularController).IsAssignableFrom(p) && !p.IsAbstract)
                .ToDictionary(
                    x => x.GetCustomAttribute<PipeRouteAttribute>(false)?.Route ?? x.Name.Replace("Controller", "").ToLowerInvariant(),
                    x => x);
        }

        public void StartListening(CancellationToken cancellationToken)
        {
            if (_funicularServer.IsTerminated)
                throw new FunicularException("Server client was terminated");
            
            Task.Factory.StartNew(
                () => _funicularServer.ReceivingLoop(HandlePipeRequest, cancellationToken), TaskCreationOptions.LongRunning);
        }

        private async Task HandlePipeRequest(FunicularMessage funicularMessage, CancellationToken cancellationToken)
        {
            if (funicularMessage.Route == FunicularMessage.EmptyRoute)
                throw new FunicularPipeRouterException(_funicularServer.PipeName, funicularMessage.Route, "Requested empty route");
            
            var parts = funicularMessage.Route
                .Split('/')
                .Where(x => !string.IsNullOrEmpty(x))
                .Select(x => x.ToLowerInvariant())
                .ToArray();

            if (parts.Length < 2)
                throw new FunicularPipeRouterException(_funicularServer.PipeName, funicularMessage.Route, "Invalid path (#1)");
            
            if (!_baseRoutePaths.TryGetValue(parts[0], out var controllerType))
                throw new FunicularPipeRouterException(_funicularServer.PipeName, funicularMessage.Route, "Invalid path (#2)");

            using var scope = _serviceProvider.CreateScope();
            var controller = scope.ServiceProvider.GetRequiredService(controllerType) as FunicularController;
            await controller!.HandlePipeRequest(funicularMessage, string.Join('/', parts.Skip(1)));
            
            if (!funicularMessage.IsPost)
                await _funicularServer.SendAsync(controller.ResponseMessage, cancellationToken);
        }

        public void Dispose()
        {
            _funicularServer.Dispose();
        }

        public ValueTask DisposeAsync()
        {
            return _funicularServer.DisposeAsync();
        }
    }
}