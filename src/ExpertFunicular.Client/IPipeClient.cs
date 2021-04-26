using System;
using ExpertFunicular.Common.Messaging;

namespace ExpertFunicular.Client
{
    internal interface IPipeClient : IDisposable
    {
        bool IsDisposed { get; }
        bool ReadMessage(out FunicularMessage message, int timeoutMs = 60_000);
        void Send(FunicularMessage message);
        void SetErrorHandler(Action<string, Exception> errorHandler);
    }
}