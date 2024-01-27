// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using BaseProtocol.Protocol;
using StreamJsonRpc;

namespace BaseProtocol.Handlers;

public class BpServiceLifeCycleManager : ILifeCycleManager, IBpService
{
    private readonly IBaseProtocolClientManager _baseProtocolClientManager;

    public BpServiceLifeCycleManager(IBaseProtocolClientManager baseProtocolClientManager)
    {
        _baseProtocolClientManager = baseProtocolClientManager;
    }

    public async Task ShutdownAsync(string message = "Shutting down")
    {
        try
        {
            var messageParams = new LogMessageParams()
            {
                MessageType = MessageType.Info,
                Message = message
            };
            await _baseProtocolClientManager.SendNotificationAsync(Methods.WindowLogMessage, messageParams, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is ObjectDisposedException or ConnectionLostException)
        {
            //Don't fail shutdown just because jsonrpc has already been cancelled.
        }
    }

    public Task ExitAsync()
    {
        // We don't need any custom logic to run on exit.
        return Task.CompletedTask;
    }
}

