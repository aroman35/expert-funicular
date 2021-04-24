using System;
using System.Linq;
using System.Security.Cryptography;
using ExpertFunicular.Common.Exceptions;
using ExpertFunicular.Common.Serializers;
using ProtoBuf;

namespace ExpertFunicular.Common.Messaging
{
    [ProtoContract]
    public sealed class FunicularMessage
    {
        [ProtoMember(1)] public string Route { get; set; } = EmptyRoute;
        [ProtoMember(2)] public DateTime CreatedTimeUtc { get; set; }
        [ProtoMember(3)] public FunicularMessageType MessageType { get; set; }
        [ProtoMember(4)] public byte[] CompressedMessage { get; private set; }
        [ProtoMember(5)] public string ErrorMessage { get; set; }
        [ProtoMember(6)] public bool IsPost { get; set; }
        [ProtoMember(7)] public ContentType Content { get; private set; } = ContentType.Protobuf;
        [ProtoMember(8)] public string Md5Hash { get; private set; }
        [ProtoMember(9)] public string PipeName { get; set; }
        [ProtoIgnore] public bool IsError => !string.IsNullOrEmpty(ErrorMessage);
        [ProtoIgnore] public bool HasValue => CompressedMessage != null && CompressedMessage.Any();
        [ProtoIgnore] public static readonly string EmptyRoute = "empty";
        [ProtoIgnore] public static readonly FunicularMessage Default = new()
        {
            Route = EmptyRoute
        };

        public object GetPayload(Type messageType)
        {
            if (!HasValue)
                throw new Exception("Message is empty");
            if (!IsValid())
                throw new Exception("Message is invalid");
            
            IFunicularDeserializer deserializer = Content switch
            {
                ContentType.Protobuf => new FunicularProtobufDeserializer(),
                ContentType.Json => new FunicularJsonDeserializer(),
                ContentType.Text => new FunicularTextDeserializer(),
                ContentType.NotSet => throw new FunicularException("Content type is not set"),
                _ => throw new FunicularException("Content type is not set")
            };

            return deserializer.Deserialize(messageType, CompressedMessage);
        }

        public void SetPayload(object payload, ContentType content = ContentType.Protobuf, bool onErrorUseJson = true)
        {
            Content = content;
            IFunicularSerializer serializer = Content switch
            {
                ContentType.Protobuf => new FunicularProtobufSerializer(),
                ContentType.Json => new FunicularJsonSerializer(),
                ContentType.Text => new FunicularTextSerializer(),
                ContentType.NotSet => throw new FunicularException("Content type is not set"),
                _ => throw new FunicularException("Content type is not set")
            };
            try
            {
                CompressedMessage = serializer.Serialize(payload);
            }
            catch
            {
                if (onErrorUseJson)
                {
                    CompressedMessage = new FunicularJsonSerializer().Serialize(payload);
                    Content = ContentType.Json;
                }
            }

            GetHash();
        }
        
        private string GetHash()
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            var hashes = md5.ComputeHash(CompressedMessage);
            return Md5Hash = hashes.Aggregate("", (current, b) => current + b.ToString("x2"));
        }
        
        private bool IsValid()
        {
            return Md5Hash == GetHash();
        }
    }
}