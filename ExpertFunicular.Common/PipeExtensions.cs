using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using ExpertFunicular.Common.Messaging;
using ExpertFunicular.Common.Serializers;

namespace ExpertFunicular.Common
{
    public static class PipeExtensions
    {
        private static readonly IFunicularSerializer FunicularSerializer;
        private static readonly IFunicularDeserializer FunicularDeserializer;

        static PipeExtensions()
        {
            FunicularSerializer = new FunicularProtobufSerializer();
            FunicularDeserializer = new FunicularProtobufDeserializer();
        }

        public static void WriteMessage(this PipeStream pipeStream, FunicularMessage message)
        {
            var compressedMessage = FunicularSerializer.Serialize(message);
            var messageSizeCompressed = BitConverter.GetBytes(compressedMessage.Length);
            var md5Compressed = Encoding.UTF8.GetBytes(message.Md5Hash);

            pipeStream.Write(messageSizeCompressed);
            pipeStream.Write(compressedMessage);
            pipeStream.Write(md5Compressed);
        }

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
    }
}