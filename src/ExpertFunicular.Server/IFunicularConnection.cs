using System;
using System.Threading;

namespace ExpertFunicular.Server
{
    public interface IFunicularConnection : IDisposable, IAsyncDisposable
    {
        void StartListening(CancellationToken cancellationToken);
        void StartListening(Action<Exception, string> errorHandler, CancellationToken cancellationToken);
    }
}