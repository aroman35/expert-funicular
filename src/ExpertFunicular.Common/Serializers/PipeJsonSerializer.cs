using System.Text;
using Newtonsoft.Json;

namespace ExpertFunicular.Common.Serializers
{
    public class PipeJsonSerializer : IPipeSerializer
    {
        public byte[] Serialize<TMessage>(TMessage message) where TMessage : class
        {
            var json = JsonConvert.SerializeObject(message);
            return Encoding.UTF8.GetBytes(json);
        }

        public byte[] Serialize(object message)
        {
            var json = JsonConvert.SerializeObject(message);
            return Encoding.UTF8.GetBytes(json);
        }
    }
}