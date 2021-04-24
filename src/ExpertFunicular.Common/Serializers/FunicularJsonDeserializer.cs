using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace ExpertFunicular.Common.Serializers
{
    public class FunicularJsonDeserializer : IFunicularDeserializer
    {
        public TMessage Deserialize<TMessage>(byte[] array) where TMessage : class
        {
            var json = Encoding.UTF8.GetString(array);
            return JsonConvert.DeserializeObject<TMessage>(json);
        }

        public TMessage Deserialize<TMessage>(Stream encoded) where TMessage : class
        {
            using var memoryStream = new MemoryStream();
            encoded.CopyTo(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);
            return Deserialize<TMessage>(memoryStream.ToArray());
        }

        public object Deserialize(Type type, byte[] array)
        {
            var json = Encoding.UTF8.GetString(array);
            return JsonConvert.DeserializeObject(json, type);
        }
    }
}