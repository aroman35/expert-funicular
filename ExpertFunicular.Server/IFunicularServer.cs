﻿using System;
using System.Threading;
using System.Threading.Tasks;
using ExpertFunicular.Common.Messaging;

namespace ExpertFunicular.Server
{
    internal interface IFunicularServer : IDisposable, IAsyncDisposable
    {
        string PipeName { get; }
        bool IsConnected { get; }
        bool IsTerminated { get; }
        bool IsDisposed { get; }
        Task ReceivingLoop(Func<FunicularMessage, CancellationToken, Task> payloadHandler, CancellationToken cancellationToken = default);
        Task ListenPipe(Func<FunicularMessage, CancellationToken, Task> payloadHandler, CancellationToken cancellationToken = default);
        Task SendAsync(FunicularMessage message, CancellationToken cancellationToken = default);
        void SetErrorHandler(Action<Exception, string> handler);
        bool ReadMessageCommon(out FunicularMessage message);
        void Send(FunicularMessage message);
    }
}