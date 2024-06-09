using BaseProtocol;
using bsp4csharp.Protocol;

namespace dotnet_bsp;

public static class IBaseProtocolClientManagerExtensions
{
    public static void SendClearDiagnosticsMessage(this IBaseProtocolClientManager clientManager)
    {
        var diagParams = new PublishDiagnosticsParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = UriFixer.WithFileSchema("/") },
            BuildTarget = new BuildTargetIdentifier { Uri = UriFixer.WithFileSchema("/") },
            Reset = true
        };

        var _ = clientManager.SendNotificationAsync(
            Methods.BuildPublishDiagnostics, diagParams, CancellationToken.None);
    }
}
