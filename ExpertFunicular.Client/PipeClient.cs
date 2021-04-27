using System;
using System.IO.Pipes;
using ExpertFunicular.Common;
using ExpertFunicular.Common.Messaging;

namespace ExpertFunicular.Client
{
    internal class PipeClient : IPipeClient
    {
        private readonly NamedPipeClientStream _pipeClient;
        private readonly string _pipeName;
        private Action<string, Exception> _errorHandler;
        
        public bool IsDisposed { get; private set; }

        public PipeClient(string pipeName)
        {
            _pipeName = pipeName;
            _pipeClient = new NamedPipeClientStream(
                ".",
                pipeName,
                PipeDirection.InOut);
        }

        public void Send(FunicularMessage message)
        {
            message.PipeName = _pipeName;
            message.CreatedTimeUtc = DateTime.UtcNow;
            
            if (!_pipeClient.IsConnected)
                _pipeClient.Connect();
            
            if (_pipeClient.CanWrite)
                _pipeClient.WriteMessage(message);
        }
        
        public bool ReadMessage(out FunicularMessage message)
        {
            if (_pipeClient.CanRead)
                return _pipeClient.ReadMessage(out message);
            
            message = FunicularMessage.Default;
            return false;
        }

        public void SetErrorHandler(Action<string, Exception> errorHandler)
        {
            _errorHandler = errorHandler;
        }
        
        public void Dispose()
        {
            _pipeClient.Dispose();
            IsDisposed = true;
        }
    }
}