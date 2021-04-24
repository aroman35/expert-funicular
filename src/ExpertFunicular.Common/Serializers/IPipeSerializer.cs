namespace ExpertFunicular.Common.Serializers
{
    public interface IPipeSerializer
    {
        byte[] Serialize<TMessage>(TMessage message)
            where TMessage : class;

        byte[] Serialize(object message);
    }
}