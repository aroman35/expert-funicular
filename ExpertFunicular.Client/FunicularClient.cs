using System;
using ExpertFunicular.Common.Exceptions;
using ExpertFunicular.Common.Messaging;

namespace ExpertFunicular.Client
{
    public class FunicularClient : IFunicularClient
    {
        private readonly IPipeClient _pipeClient;
        private readonly object _sync = new();

        internal FunicularClient(IPipeClient pipeClient)
        {
            _pipeClient = pipeClient;
        }

        public TResponse Send<TRequest, TResponse>(string uri, TRequest request, int timeoutMs = 30_000, ContentType desiredContent = ContentType.Protobuf)
        {
            var requestMessage = new FunicularMessage();
            requestMessage.SetPayload(request);
            requestMessage.MessageType = FunicularMessageType.Request;
            requestMessage.CreatedTimeUtc = DateTime.UtcNow;
            requestMessage.Route = uri;

            lock (_sync)
            {
                _pipeClient.Send(requestMessage);
                
                if (!_pipeClient.ReadMessageCommon(out var responseMessage))
                    throw new FunicularPipeException($"Nothing received within {timeoutMs} ms", requestMessage.Route);

                return HandleResponse<TResponse>(responseMessage);
            }
        }

        public void Send<TRequest>(string uri, TRequest request, ContentType desiredContent = ContentType.Protobuf)
        {
            var requestMessage = new FunicularMessage();
            requestMessage.SetPayload(request, desiredContent);
            requestMessage.MessageType = FunicularMessageType.Request;
            requestMessage.CreatedTimeUtc = DateTime.UtcNow;
            requestMessage.Route = uri;
            requestMessage.IsPost = true;
            
            lock (_sync) _pipeClient.Send(requestMessage);
        }

        public TResponse Get<TResponse>(string uri, int timeoutMs = 30_000, ContentType desiredContent = ContentType.Protobuf)
        {
            var requestMessage = new FunicularMessage();
            requestMessage.MessageType = FunicularMessageType.Request;
            requestMessage.CreatedTimeUtc = DateTime.UtcNow;
            requestMessage.Route = uri;

            lock (_sync)
            {
                _pipeClient.Send(requestMessage);
                
                if (!_pipeClient.ReadMessageCommon(out var responseMessage))
                    throw new FunicularPipeException($"Nothing received within {timeoutMs} ms", requestMessage.Route);

                return HandleResponse<TResponse>(responseMessage);
            }
        }

        public void SetErrorHandler(Action<string, Exception> errorHandler)
        {
            lock (_sync)
            {
                _pipeClient.SetErrorHandler(errorHandler);
            }
        }

        private TResponse HandleResponse<TResponse>(FunicularMessage pipeMessage)
        {
            if (pipeMessage.IsError)
                throw new FunicularPipeException(pipeMessage.ErrorMessage, pipeMessage.Route);
            if (pipeMessage.MessageType == FunicularMessageType.Request)
                throw new FunicularPipeException("Received response but expected request", pipeMessage.Route);
                
            return (TResponse) pipeMessage.GetPayload(typeof(TResponse));
        }

        /// <summary>
        /// When it is called disposed, the pipe stream disposes too. If it is the only client for the pipe, the pipe becomes broken
        /// </summary>
        public void Dispose()
        {
            _pipeClient.Dispose();
        }
    }
}