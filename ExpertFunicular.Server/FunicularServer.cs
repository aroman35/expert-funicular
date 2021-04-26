using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.Versioning;
using System.Text;
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
                _errorHandler?.Invoke(ioException, $"Pipe is broken {PipeName}");
            }
            catch (Exception exception)
            {
                _errorHandler?.Invoke(exception, $"Error listening pipe {PipeName}");
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

                    var testMessage = await ReadMessageAsync(cancellationToken);
                    await payloadHandler(testMessage, cancellationToken);
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

        public async Task ListenPipe(Func<FunicularMessage, CancellationToken, Task> payloadHandler, CancellationToken cancellationToken = default)
        {
            while (!cancellationToken.IsCancellationRequested && !IsTerminated)
            {
                try
                {
                    if (!_pipeServer.IsConnected)
                        await _pipeServer.WaitForConnectionAsync(cancellationToken);

                    if (ReadMessageCommon(out var message))
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

        private async Task<FunicularMessage> ReadMessageAsync(CancellationToken cancellationToken)
        {
            _pipeServer.ReadMode = PipeTransmissionMode.Message;

            try
            {
                if (_pipeServer.CanRead)
                {
                    var memoryStream = new MemoryStream();
                    do
                    {
                        var memoryBuffer = new Memory<byte>(new byte[16]);
                        var bytesRead = await _pipeServer.ReadAsync(memoryBuffer, cancellationToken);
                        if (bytesRead == -1)
                        {
                            Debugger.Launch();
                        }
                        await memoryStream.WriteAsync(memoryBuffer[..bytesRead], cancellationToken);
                    } while (!_pipeServer.IsMessageComplete);

                    if (memoryStream.CanSeek)
                        memoryStream.Seek(0, SeekOrigin.Begin);
                    return _deserializer.Deserialize<FunicularMessage>(memoryStream);
                }
            }
            catch (Exception exception)
            {
                _errorHandler?.Invoke(exception, PipeName);
                return FunicularMessage.Default;
            }

            return FunicularMessage.Default;
        }

        public unsafe bool ReadMessageCommon(out FunicularMessage message)
        {
            message = FunicularMessage.Default;
            
            _pipeServer.ReadMode = PipeTransmissionMode.Byte;
            Span<byte> messageSizeBuffer = stackalloc byte[4]; // int32 - the size of the message

            for (var i = 0; i < 4; i++)
            {
                var readByte = _pipeServer.ReadByte();
                if (readByte == -1) return false;
                messageSizeBuffer[i] = (byte) _pipeServer.ReadByte();
            }

            var messageSize = BitConverter.ToInt32(messageSizeBuffer);
            
            var messageEncoded = messageSize < 1024 * 1024 // >1Mb - Stack
                ? stackalloc byte[messageSize]
                : new Span<byte>(new byte[messageSize]);
            
            for (var i = 0; i < messageSize; i++)
            {
                var readByte = _pipeServer.ReadByte();
                if (readByte == -1) return false;
                messageEncoded[i] = (byte) readByte;
            }

            try
            {
                Serializer.Deserialize(messageEncoded, message);
            }
            catch
            {
                return false;
            }
            
            Span<byte> hashBuffer = stackalloc byte[128 / 8]; // md5 is 128 bit length. Means the end of message
            
            for (var i = 0; i < 16; i++)
            {
                var readByte = _pipeServer.ReadByte();
                if (readByte == -1) return false;
                hashBuffer[i] = (byte) _pipeServer.ReadByte();
            }

            var receivedMd5 = Encoding.UTF8.GetString(hashBuffer);
            return receivedMd5 == message.Md5Hash;
        }
        
        public unsafe void Send(FunicularMessage message)
        {
            message.PipeName = PipeName;
            message.CreatedTimeUtc = DateTime.UtcNow;
            
            if (!_pipeServer.IsConnected)
                _pipeServer.WaitForConnection();
            
            var compressedMessage = new Span<byte>(_serializer.Serialize(message));
            fixed (byte* sizePtr = compressedMessage)
                _pipeServer.WriteByte(*sizePtr);
            
            var messageSizeCompressed = new Span<byte>(BitConverter.GetBytes(compressedMessage.Length));
            fixed (byte* msgPtr = messageSizeCompressed)
                _pipeServer.WriteByte(*msgPtr);
            
            var md5Compressed = new Span<byte>(Encoding.UTF8.GetBytes(message.Md5Hash));
            fixed (byte* md5Ptr = md5Compressed)
                _pipeServer.WriteByte(*md5Ptr);
        }
        
        [Obsolete("Use async ReadMessageAsync. Marked for deletion")]
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
            catch (Exception exception)
            {
                _errorHandler?.Invoke(exception, PipeName);
                message = FunicularMessage.Default;
                return false;
            }
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