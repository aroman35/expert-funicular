﻿using System;
using ExpertFunicular.Common.Exceptions;
using ExpertFunicular.Common.Messaging;

namespace ExpertFunicular.Client
{
    public class FunicularClient : IDisposable
    {
        private readonly PipeClient _pipeClient;

        public FunicularClient(string pipeName)
        {
            _pipeClient = new PipeClient(pipeName);
        }

        public TResponse Send<TRequest, TResponse>(string uri, TRequest request, int timeoutMs = 30_000, ContentType desiredContent = ContentType.Protobuf)
        {
            var requestMessage = new FunicularMessage();
            requestMessage.SetPayload(request);
            requestMessage.MessageType = FunicularMessageType.Request;
            requestMessage.CreatedTimeUtc = DateTime.UtcNow;
            requestMessage.Route = uri;
            requestMessage.Content = desiredContent;
            
            _pipeClient.Send(requestMessage);

            if (!_pipeClient.ReadMessage(out var responseMessage, timeoutMs))
                throw new FunicularPipeException($"Nothing received within {timeoutMs} ms", requestMessage.Route);

            return HandleResponse<TResponse>(responseMessage);
        }

        public void Send<TRequest>(string uri, TRequest request, ContentType desiredContent = ContentType.Protobuf)
        {
            var requestMessage = new FunicularMessage();
            requestMessage.SetPayload(request);
            requestMessage.MessageType = FunicularMessageType.Request;
            requestMessage.CreatedTimeUtc = DateTime.UtcNow;
            requestMessage.Route = uri;
            requestMessage.IsPost = true;
            requestMessage.Content = desiredContent;

            _pipeClient.Send(requestMessage);
        }

        public TResponse Get<TResponse>(string uri, int timeoutMs = 30_000, ContentType desiredContent = ContentType.Protobuf)
        {
            var requestMessage = new FunicularMessage();
            requestMessage.MessageType = FunicularMessageType.Request;
            requestMessage.CreatedTimeUtc = DateTime.UtcNow;
            requestMessage.Route = uri;
            requestMessage.Content = desiredContent;

            _pipeClient.Send(requestMessage);
            
            if (!_pipeClient.ReadMessage(out var responseMessage, timeoutMs))
                throw new FunicularPipeException($"Nothing received within {timeoutMs} ms", requestMessage.Route);

            return HandleResponse<TResponse>(responseMessage);
        }

        private TResponse HandleResponse<TResponse>(FunicularMessage pipeMessage)
        {
            if (pipeMessage.IsError)
                throw new FunicularPipeException(pipeMessage.ErrorMessage, pipeMessage.Route);
            if (pipeMessage.MessageType == FunicularMessageType.Request)
                throw new FunicularPipeException("Received response but expected request", pipeMessage.Route);
                
            return (TResponse) pipeMessage.GetPayload(typeof(TResponse));
        }

        public void Dispose()
        {
            _pipeClient.Dispose();
        }
    }
}