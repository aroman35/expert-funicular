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
    public class PipeRouter : IDisposable
    {
        private readonly IDictionary<string, Type> _baseRoutePaths;
        private readonly IServiceProvider _serviceProvider;
        private readonly PipeServer _pipeServer;

        public PipeRouter(
            PipeServer pipeServer,
            IServiceProvider serviceProvider)
        {
            _pipeServer = pipeServer;
            _serviceProvider = serviceProvider;
            _baseRoutePaths = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => typeof(PipeController).IsAssignableFrom(p) && !p.IsAbstract)
                .ToDictionary(
                    x => x.GetCustomAttribute<PipeRouteAttribute>(false)?.Route ?? x.Name.Replace("Controller", "").ToLowerInvariant(),
                    x => x);
        }

        public void StartListening(CancellationToken cancellationToken)
        {
            Task.Factory.StartNew(
                () => _pipeServer.ReceivingLoop(HandlePipeRequest, cancellationToken), TaskCreationOptions.LongRunning);
        }

        private async Task HandlePipeRequest(FunicularMessage funicularMessage, CancellationToken cancellationToken)
        {
            if (funicularMessage.Route == FunicularMessage.EmptyRoute)
                throw new FunicularPipeRouterException(_pipeServer.PipeName, funicularMessage.Route, "Requested empty route");
            
            var parts = funicularMessage.Route
                .Split('/')
                .Where(x => !string.IsNullOrEmpty(x))
                .Select(x => x.ToLowerInvariant())
                .ToArray();

            if (parts.Length < 2)
                throw new FunicularPipeRouterException(_pipeServer.PipeName, funicularMessage.Route, "Invalid path (#1)");
            
            if (!_baseRoutePaths.TryGetValue(parts[0], out var controllerType))
                throw new FunicularPipeRouterException(_pipeServer.PipeName, funicularMessage.Route, "Invalid path (#2)");

            using var scope = _serviceProvider.CreateScope();
            var controller = scope.ServiceProvider.GetRequiredService(controllerType) as PipeController;
            await controller!.HandlePipeRequest(funicularMessage, string.Join('/', parts.Skip(1)));
            
            if (!controller.RequestMessage.IsPost)
                await _pipeServer.SendAsync(controller.ResponseMessage, cancellationToken);
        }
        
        public void Dispose()
        {
            _pipeServer.Dispose();
        }
    }
}