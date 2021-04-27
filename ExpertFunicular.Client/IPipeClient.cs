using System;
using ExpertFunicular.Common.Messaging;

namespace ExpertFunicular.Client
{
    internal interface IPipeClient : IDisposable
    {
        bool IsDisposed { get; }
        void Send(FunicularMessage message);
        void SetErrorHandler(Action<string, Exception> errorHandler);
        bool ReadMessage(out FunicularMessage message);
    }
}