namespace ExpertFunicular.Common.Serializers
{
    public interface IFunicularSerializer
    {
        byte[] Serialize<TMessage>(TMessage message)
            where TMessage : class;

        byte[] Serialize(object message);
    }
}