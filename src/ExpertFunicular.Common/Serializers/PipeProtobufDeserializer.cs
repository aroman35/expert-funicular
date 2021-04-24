using System;
using System.IO;
using ProtoBuf;

namespace ExpertFunicular.Common.Serializers
{
    public class PipeProtobufDeserializer : IPipeDeserializer
    {
        public TMessage Deserialize<TMessage>(byte[] array) where TMessage : class
        {
            using var memoryStream = new MemoryStream(array);
            return Deserialize<TMessage>(memoryStream);
        }

        public TMessage Deserialize<TMessage>(Stream encoded) where TMessage : class
        {
            var instance = Serializer.Deserialize<TMessage>(encoded);
            return instance;
        }

        public object Deserialize(Type type, byte[] array)
        {
            using var memoryStream = new MemoryStream(array);
            return Serializer.Deserialize(type, memoryStream);
        }
    }
}