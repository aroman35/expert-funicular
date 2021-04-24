using System;
using System.Text;

namespace ExpertFunicular.Common.Serializers
{
    public class PipeTextSerializer : IPipeSerializer
    {
        public byte[] Serialize<TMessage>(TMessage message) where TMessage : class
        {
            if (message is string stringMessage)
                return Encoding.UTF8.GetBytes(stringMessage);
            throw new ArgumentException("Only strings are supported");
        }

        public byte[] Serialize(object message)
        {
            if (message is string stringMessage)
                return Encoding.UTF8.GetBytes(stringMessage);
            throw new ArgumentException("Only strings are supported");
        }
    }
}