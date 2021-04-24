using ExpertFunicular.Common.Messaging;

namespace ExpertFunicular.Client
{
    public interface IFunicularClient
    {
        TResponse Send<TRequest, TResponse>(string uri, TRequest request, int timeoutMs = 30_000, ContentType desiredContent = ContentType.Protobuf);
        void Send<TRequest>(string uri, TRequest request, ContentType desiredContent = ContentType.Protobuf);
        TResponse Get<TResponse>(string uri, int timeoutMs = 30_000, ContentType desiredContent = ContentType.Protobuf);
    }
}