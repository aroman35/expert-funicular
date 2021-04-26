using System;
using System.IO;
using System.Text;

namespace ExpertFunicular.Common.Serializers
{
    public class FunicularTextDeserializer : IFunicularDeserializer
    {
        public TMessage Deserialize<TMessage>(byte[] array) where TMessage : class
        {
            if (typeof(TMessage) == typeof(string))
                return Encoding.UTF8.GetString(array) as TMessage;
            throw new ArgumentException("Only strings are supported");
        }

        public TMessage Deserialize<TMessage>(Stream encoded) where TMessage : class
        {
            using var memoryStream = new MemoryStream();
            encoded.CopyTo(memoryStream);
            throw new ArgumentException("Only strings are supported");
        }

        public object Deserialize(Type type, byte[] array)
        {
            if (type == typeof(string))
                return Encoding.UTF8.GetString(array);
            throw new ArgumentException("Only strings are supported");
        }
    }
}