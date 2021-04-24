using System;
using ExpertFunicular.Common.Serializers;
using Shouldly;
using Xunit;

namespace ExpertFunicular.UnitTests.Serializers
{
    public class TextSerializerTests
    {
        private readonly IFunicularSerializer _serializer;
        private readonly IFunicularDeserializer _deserializer;
        
        private const string SingleLineMessage = "Lorem Ipsum is simply dummy text of the printing and typesetting industry.";
        private const string MultiLineMessage = @"It is a long established fact that a reader will be distracted by
 the readable content of a page when looking at its layout. The point of using Lorem Ipsum is that it has a more-or-less
 normal distribution of letters, as opposed to using 'Content here, content here', making it look like readable English.
 Many desktop publishing packages and web page editors now use Lorem Ipsum as their default model text, and a search for
 'lorem ipsum' will uncover many web sites still in their infancy. Various versions have evolved over the years, sometimes
 by accident, sometimes on purpose (injected humour and the like).";
        

        public TextSerializerTests()
        {
            _serializer = new FunicularTextSerializer();
            _deserializer = new FunicularTextDeserializer();
        }

        [Fact(DisplayName = "Text serializer: Serialize single line of text as string, deserialize as string. Success result")]
        public void SingleLineStringSerializeAsStringAndDeserializeAsString_Success()
        {
            var serializedMessage = _serializer.Serialize(SingleLineMessage);
            var deserializedMessage = _deserializer.Deserialize<string>(serializedMessage);
            
            deserializedMessage.ShouldBe(SingleLineMessage);
        }
        
        [Fact(DisplayName = "Text serializer: Serialize single line of text as string, deserialize as object. Success result")]
        public void SingleLineStringSerializeAsStringAndDeserializeAsObject_Success()
        {
            var serializedMessage = _serializer.Serialize(SingleLineMessage);
            var deserializedMessage = _deserializer.Deserialize(typeof(string), serializedMessage);
            
            deserializedMessage.ShouldBe(SingleLineMessage);
        }
        
        [Fact(DisplayName = "Text serializer: Serialize single line of text as object, deserialize as string. Success result")]
        public void SingleLineStringSerializeAsObjectAndDeserializeAsString_Success()
        {
            var serializedMessage = _serializer.Serialize((object)SingleLineMessage);
            var deserializedMessage = _deserializer.Deserialize<string>(serializedMessage);

            deserializedMessage.ShouldBe(SingleLineMessage);
        }
        
        [Fact(DisplayName = "Text serializer: Serialize single line of text as object, deserialize as object. Success result")]
        public void SingleLineStringSerializeAsObjectAndDeserializeAsObject_Success()
        {
            var serializedMessage = _serializer.Serialize((object)SingleLineMessage);
            var deserializedMessage = _deserializer.Deserialize(typeof(string), serializedMessage);

            deserializedMessage.ShouldBe(SingleLineMessage);
        }
        
        [Fact(DisplayName = "Text serializer: Serialize multi line of text as string, deserialize as string. Success result")]
        public void MultiLineStringSerializeAsStringAndDeserializeAsString_Success()
        {
            var serializedMessage = _serializer.Serialize(MultiLineMessage);
            var deserializedMessage = _deserializer.Deserialize<string>(serializedMessage);
            
            deserializedMessage.ShouldBe(MultiLineMessage);
        }
        
        [Fact(DisplayName = "Text serializer: Serialize multi line of text as string, deserialize as object. Success result")]
        public void MultiLineStringSerializeAsStringAndDeserializeAsObject_Success()
        {
            var serializedMessage = _serializer.Serialize(MultiLineMessage);
            var deserializedMessage = _deserializer.Deserialize(typeof(string), serializedMessage);
            
            deserializedMessage.ShouldBe(MultiLineMessage);
        }
        
        [Fact(DisplayName = "Text serializer: Serialize multi line of text as object, deserialize as string. Success result")]
        public void MultiLineStringSerializeAsObjectAndDeserializeAsString_Success()
        {
            var serializedMessage = _serializer.Serialize((object)MultiLineMessage);
            var deserializedMessage = _deserializer.Deserialize<string>(serializedMessage);

            deserializedMessage.ShouldBe(MultiLineMessage);
        }
        
        [Fact(DisplayName = "Text serializer: Serialize multi line of text as object, deserialize as object. Success result")]
        public void MultiLineStringSerializeAsObjectAndDeserializeAsObject_Success()
        {
            var serializedMessage = _serializer.Serialize((object)MultiLineMessage);
            var deserializedMessage = _deserializer.Deserialize(typeof(string), serializedMessage);

            deserializedMessage.ShouldBe(MultiLineMessage);
        }

        [Fact(DisplayName = "Text serializer: Serialize not text type as type. Throws an exception")]
        public void NotStringSerializeAsIs_ThrowsArgumentException()
        {
            Should.Throw<ArgumentException>(() => _serializer.Serialize(new object()))
                .Message.ShouldBe("Only strings are supported");
        }
        
        [Fact(DisplayName = "Text serializer: Serialize not text type as type. Throws an exception")]
        public void NotStringSerializeAsObject_ThrowsArgumentException()
        {
            Should.Throw<ArgumentException>(() => _serializer.Serialize<object>(new object()))
                .Message.ShouldBe("Only strings are supported");
        }
    }
}