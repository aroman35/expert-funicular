using System;
using System.IO;

namespace ExpertFunicular.Common.Serializers
{
    public interface IFunicularDeserializer
    {
        TMessage Deserialize<TMessage>(byte[] array)
            where TMessage : class;

        TMessage Deserialize<TMessage>(Stream encoded) where TMessage : class;

        object Deserialize(Type type, byte[] array);
    }
}