using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using ExpertFunicular.Common.Messaging;
using ExpertFunicular.Common.Serializers;

namespace ExpertFunicular.Common
{
    public static unsafe class PipeExtensions
    {
        private static readonly IFunicularSerializer FunicularSerializer;
        private static readonly IFunicularDeserializer FunicularDeserializer;

        static PipeExtensions()
        {
            FunicularSerializer = new FunicularProtobufSerializer();
            FunicularDeserializer = new FunicularProtobufDeserializer();
        }

        [Obsolete("Use unsafe")]
        // TODO: compare performance with unsafe
        public static void WriteMessage(this PipeStream pipeStream, FunicularMessage message)
        {
            var compressedMessage = FunicularSerializer.Serialize(message);
            var messageSizeCompressed = BitConverter.GetBytes(compressedMessage.Length);
            var md5Compressed = Encoding.UTF8.GetBytes(message.Md5Hash);

            pipeStream.Write(messageSizeCompressed);
            pipeStream.Write(compressedMessage);
            pipeStream.Write(md5Compressed);
        }
        [Obsolete("Use unsafe")]
        // TODO: compare performance with unsafe
        public static bool ReadMessage(this PipeStream pipeStream, out FunicularMessage message)
        {
            var compressedSize = pipeStream.Read(4);
            var size = BitConverter.ToInt32(compressedSize);
            var compressedMessage = pipeStream.Read(size);
            message = FunicularDeserializer.Deserialize<FunicularMessage>(compressedMessage);
            var md5Compressed = pipeStream.Read(32);
            var md5 = Encoding.UTF8.GetString(md5Compressed);
            return md5 == message.Md5Hash;
        }

        private static byte[] Read(this Stream pipeStream, int length)
        {
            var buffer = new byte[length];
            pipeStream.Read(buffer, 0, length);

            return buffer;
        }

        public static void WriteMessageUnsafe(this PipeStream pipeStream, FunicularMessage message)
        {
            var compressedMessage = FunicularSerializer.Serialize(message);
            var messageSizeCompressed = BitConverter.GetBytes(compressedMessage.Length);
            var md5Compressed = Encoding.UTF8.GetBytes(message.Md5Hash);

            pipeStream.WriteMessageBlockUnsafe(messageSizeCompressed);
            pipeStream.WriteMessageBlockUnsafe(compressedMessage);
            pipeStream.WriteMessageBlockUnsafe(md5Compressed);
        }

        public static bool ReadMessageUnsafe(this PipeStream pipeStream, out FunicularMessage message)
        {
            if (!pipeStream.CanWrite)
            {
                message = FunicularMessage.Default;
                return false;
            }

            var messageSize = pipeStream.ReadSizeOfMessageUnsafe();
            message = pipeStream.ReadMessageUnsafe(messageSize);
            var md5 = pipeStream.ReadMd5Unsafe();
            return md5 == message.Md5Hash;
        }

        private static void WriteMessageBlockUnsafe(this Stream destinationStream, byte[] message)
        {
            fixed(byte* arrPtr = message)
                for (var i = 0; i < message.Length; i++)
                    destinationStream.WriteByte(arrPtr[i]);
        }

        private static int ReadSizeOfMessageUnsafe(this Stream sourceStream)
        {
            Span<byte> resultBytes = stackalloc byte[sizeof(int)];
            fixed (byte* source = resultBytes)
                for (var i = 0; i < sizeof(int); i++)
                    source[i] = (byte) sourceStream.ReadByte();
            
            var result = BitConverter.ToInt32(resultBytes);
            return result;
        }

        private static FunicularMessage ReadMessageUnsafe(this Stream sourceStream, int sizeOfMessage)
        {
            var resultBytes = sizeOfMessage < 0x00100000 ? stackalloc byte[sizeOfMessage] : new byte[sizeOfMessage];
            
            fixed (byte* ptr = resultBytes)
                for (var i = 0; i < sizeOfMessage; i++)
                    ptr[i] = (byte) sourceStream.ReadByte();

            var result = FunicularDeserializer.Deserialize<FunicularMessage>(resultBytes.ToArray());
            return result;
        }

        private static string ReadMd5Unsafe(this Stream sourceStream)
        {
            Span<byte> resultBytes = stackalloc byte[32];
            
            fixed (byte* ptr = resultBytes)
                for (var i = 0; i < 32; i++)
                    ptr[i] = (byte) sourceStream.ReadByte();

            var result = Encoding.UTF8.GetString(resultBytes);
            return result;
        }
    }
}