using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ExpertFunicular.Common;
using ExpertFunicular.Common.Messaging;
using ExpertFunicular.Common.Serializers;
using ProtoBuf;

namespace ExpertFunicular.Server
{
    internal class FunicularServer : IFunicularServer
    {
        private readonly NamedPipeServerStream _pipeServer;
        private readonly IFunicularSerializer _serializer;
        private readonly IFunicularDeserializer _deserializer;
        public string PipeName { get; }
        public bool IsConnected => _pipeServer.IsConnected;
        public bool IsTerminated { get; private set; }
        
        public bool IsDisposed { get; private set; }

        private Action<Exception, string> _errorHandler;

        public FunicularServer(string pipeName)
        {
            _pipeServer = new NamedPipeServerStream(pipeName,
                PipeDirection.InOut,
                NamedPipeServerStream.MaxAllowedServerInstances,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous);

            PipeName = pipeName;
            _serializer = new FunicularProtobufSerializer();
            _deserializer = new FunicularProtobufDeserializer();
        }

        public async Task StartListening(Func<FunicularMessage, CancellationToken, Task> payloadHandler, CancellationToken cancellationToken = default)
        {
            while (!cancellationToken.IsCancellationRequested && !IsTerminated)
            {
                try
                {
                    if (!_pipeServer.IsConnected)
                        await _pipeServer.WaitForConnectionAsync(cancellationToken);

                    if (_pipeServer.CanRead && _pipeServer.ReadMessage(out var message))
                        await payloadHandler(message, cancellationToken);
                }
                catch (IOException ioException)
                {
                    IsTerminated = true;
                    _errorHandler?.Invoke(ioException, $"Pipe is broken {PipeName}");
                }
                catch (Exception exception)
                {
                    _errorHandler?.Invoke(exception, $"Error listening pipe {PipeName}");
                }
            }
        }
        
        public void Send(FunicularMessage message)
        {
            if (_pipeServer.CanWrite)
                _pipeServer.WriteMessage(message);
        }
        
        public void SetErrorHandler(Action<Exception, string> handler)
        {
            _errorHandler = handler;
        }

        public void Dispose()
        {
            _pipeServer.Dispose();
            IsDisposed = true;
        }

        public async ValueTask DisposeAsync()
        {
            await _pipeServer.DisposeAsync();
            IsDisposed = true;
        }
    }

    
}