using System.Threading;

namespace ExpertFunicular.Server
{
    public interface IFunicularConnection
    {
        void StartListening(CancellationToken cancellationToken);
    }
}