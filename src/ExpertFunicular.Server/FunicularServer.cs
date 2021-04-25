﻿using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using ExpertFunicular.Common.Messaging;
using ExpertFunicular.Common.Serializers;
using ProtoBuf;

namespace ExpertFunicular.Server
{
    internal class FunicularServer : IFunicularServer
    {
        private readonly NamedPipeServerStream _pipeServer;
        private readonly IFunicularSerializer _serializer;
        public string PipeName { get; }
        public bool IsConnected => _pipeServer.IsConnected;
        public bool IsTerminated { get; private set; }
        
        public bool IsDisposed { get; private set; }

        private Action<Exception, string> _exceptionHandler;

        public FunicularServer(string pipeName)
        {
            _pipeServer = new NamedPipeServerStream(pipeName,
                PipeDirection.InOut,
                NamedPipeServerStream.MaxAllowedServerInstances,
                PipeTransmissionMode.Message,
                PipeOptions.Asynchronous);

            PipeName = pipeName;
            _serializer = new FunicularProtobufSerializer();
        }
        
        public async Task SendAsync(FunicularMessage message, CancellationToken cancellationToken = default)
        {
            message.PipeName = PipeName;
            try
            {
                if (!_pipeServer.IsConnected)
                    await _pipeServer.WaitForConnectionAsync(cancellationToken);
                message.CreatedTimeUtc = DateTime.UtcNow;
                
                var cmp = new ReadOnlyMemory<byte>(_serializer.Serialize(message));
                await _pipeServer.WriteAsync(cmp, cancellationToken);
            }
            catch (IOException ioException)
            {
                IsTerminated = true;
                _exceptionHandler?.Invoke(ioException, $"Pipe is broken {PipeName}");
            }
            catch (Exception exception)
            {
                _exceptionHandler?.Invoke(exception, $"Error listening pipe {PipeName}");
            }
        }

        public async Task ReceivingLoop(Func<FunicularMessage, CancellationToken, Task> payloadHandler, CancellationToken cancellationToken = default)
        {
            while (!cancellationToken.IsCancellationRequested && !IsTerminated)
            {
                try
                {
                    if (!_pipeServer.IsConnected)
                        await _pipeServer.WaitForConnectionAsync(cancellationToken);
                    if (ReadMessage(out var message))
                    {
                        await payloadHandler(message, cancellationToken);
                    }
                }
                catch (IOException ioException)
                {
                    IsTerminated = true;
                    _exceptionHandler?.Invoke(ioException, $"Pipe is broken {PipeName}");
                }
                catch (Exception exception)
                {
                    _exceptionHandler?.Invoke(exception, $"Error listening pipe {PipeName}");
                }
            }
        }
        
        private bool ReadMessage(out FunicularMessage message)
        {
            try
            {
                _pipeServer.ReadMode = PipeTransmissionMode.Message;

                if (_pipeServer.CanRead)
                {
                    using var memory = new MemoryStream();
                    do
                    {
                        var readByte = _pipeServer.ReadByte();
                        if (readByte == -1)
                            break;
                        memory.WriteByte((byte) readByte);
                    } while (!_pipeServer.IsMessageComplete);

                    if (memory.CanSeek)
                        memory.Seek(0, SeekOrigin.Begin);

                    message = Serializer.Deserialize<FunicularMessage>(memory);
                    return true;
                }
                
                message = FunicularMessage.Default;
                return false;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                message = FunicularMessage.Default;
                return false;
            }
        }

        public void SetErrorHandler(Action<Exception, string> handler)
        {
            _exceptionHandler = handler;
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