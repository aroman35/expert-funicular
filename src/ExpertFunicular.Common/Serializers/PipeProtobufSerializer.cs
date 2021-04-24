using System.IO;
using ProtoBuf;

namespace ExpertFunicular.Common.Serializers
{
    public class PipeProtobufSerializer : IPipeSerializer
    {
        public byte[] Serialize<TMessage>(TMessage message) where TMessage : class
        {
            using var memoryStream = new MemoryStream();
            Serializer.Serialize(memoryStream, message);
            return memoryStream.ToArray();
        }

        public byte[] Serialize(object message)
        {
            using var memoryStream = new MemoryStream();
            Serializer.Serialize(memoryStream, message);
            return memoryStream.ToArray();
        }
    }
}