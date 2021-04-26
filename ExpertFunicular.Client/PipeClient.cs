using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
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
            SendUnsafe(message);
            // try
            // {
            //     message.PipeName = _pipeName;
            //     if (!_pipeClient.IsConnected)
            //         _pipeClient.Connect();
            //
            //     var cmp = new ReadOnlyMemory<byte>(_serializer.Serialize(message));
            //     _pipeClient.Write(cmp.Span);
            // }
            // catch (Exception exception)
            // {
            //     _errorHandler?.Invoke(_pipeName, exception);
            //     throw;
            // }
        }
        
        public unsafe bool ReadMessageCommon(out FunicularMessage message)
        {
            message = FunicularMessage.Default;
            
            _pipeClient.ReadMode = PipeTransmissionMode.Byte;
            Span<byte> messageSizeBuffer = stackalloc byte[4]; // int32 - the size of the message

            for (var i = 0; i < 4; i++)
            {
                var readByte = _pipeClient.ReadByte();
                if (readByte == -1) return false;
                messageSizeBuffer[i] = (byte) _pipeClient.ReadByte();
            }

            var messageSize = BitConverter.ToInt32(messageSizeBuffer);
            
            var messageEncoded = messageSize < 1024 * 1024 // >1Mb - Stack
                ? stackalloc byte[messageSize]
                : new Span<byte>(new byte[messageSize]);
            
            for (var i = 0; i < messageSize; i++)
            {
                var readByte = _pipeClient.ReadByte();
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
                var readByte = _pipeClient.ReadByte();
                if (readByte == -1) return false;
                hashBuffer[i] = (byte) _pipeClient.ReadByte();
            }

            var receivedMd5 = Encoding.UTF8.GetString(hashBuffer);
            return receivedMd5 == message.Md5Hash;
        }
        
        private unsafe void SendUnsafe(FunicularMessage message)
        {
            message.PipeName = _pipeName;
            message.CreatedTimeUtc = DateTime.UtcNow;
            
            if (!_pipeClient.IsConnected)
                _pipeClient.Connect();
            
            var compressedMessage = new Span<byte>(_serializer.Serialize(message));
            var messageSizeCompressed = new Span<byte>(BitConverter.GetBytes(compressedMessage.Length));
            
            fixed (byte* msgPtr = messageSizeCompressed)
                _pipeClient.WriteByte(*msgPtr);
            
            fixed (byte* sizePtr = compressedMessage)
                _pipeClient.WriteByte(*sizePtr);

            var md5Compressed = new Span<byte>(Encoding.UTF8.GetBytes(message.Md5Hash));
            fixed (byte* md5Ptr = md5Compressed)
                _pipeClient.WriteByte(*md5Ptr);
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