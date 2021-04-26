﻿using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using ExpertFunicular.Common.Messaging;
using ExpertFunicular.Common.Serializers;
using ProtoBuf;

namespace ExpertFunicular.Client
{
    internal class PipeClient : IPipeClient
    {
        private readonly NamedPipeClientStream _pipeClient;
        private readonly IFunicularSerializer _serializer;
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
            
            _serializer = new FunicularProtobufSerializer();
        }

        public bool ReadMessage(out FunicularMessage message, int timeoutMs = 60_000)
        {
            try
            {
                var cancellationTokenSource = new CancellationTokenSource(timeoutMs);
                while (!cancellationTokenSource.Token.IsCancellationRequested)
                {
                    if (!_pipeClient.IsConnected)
                        _pipeClient.Connect(timeoutMs);

                    _pipeClient.ReadMode = PipeTransmissionMode.Message;

                    if (_pipeClient.CanRead)
                    {
                        using var memory = new MemoryStream();
                        do
                        {
                            var readByte = _pipeClient.ReadByte();
                            if (readByte == -1)
                                break;
                            memory.WriteByte((byte) readByte);
                        } while (!_pipeClient.IsMessageComplete);
                        
                        if (memory.CanSeek)
                            memory.Seek(0, SeekOrigin.Begin);
                        
                        message = Serializer.Deserialize<FunicularMessage>(memory);
                        return true;
                    }
                }
                message = FunicularMessage.Default;
                return false;
            }
            catch (Exception exception)
            {
                _errorHandler?.Invoke(_pipeName, exception);
                message = FunicularMessage.Default;
                return false;
            }
        }

        public void Send(FunicularMessage message)
        {
            try
            {
                message.PipeName = _pipeName;
                if (!_pipeClient.IsConnected)
                    _pipeClient.Connect();

                var cmp = new ReadOnlyMemory<byte>(_serializer.Serialize(message));
                _pipeClient.Write(cmp.Span);
            }
            catch (Exception exception)
            {
                _errorHandler?.Invoke(_pipeName, exception);
                throw;
            }
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