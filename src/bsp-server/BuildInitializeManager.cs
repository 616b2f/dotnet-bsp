using BaseProtocol;
using bsp4csharp.Protocol;

namespace dotnet_bsp;

internal class BuildInitializeManager : IInitializeManager<InitializeBuildParams, InitializeBuildResult>
{
    private InitializeBuildParams? _initializeParams;

    public void SetInitializeParams(InitializeBuildParams request)
    {
        _initializeParams = request;
    }

    public InitializeBuildResult GetInitializeResult()
    {
        var serverCapabilities = new BuildServerCapabilities()
        {
            CompileProvider = new CompileProvider { LanguageIds = { LanguageId.Csharp } },
            RunProvider = new RunProvider { LanguageIds = { LanguageId.Csharp } },
            TestProvider = new TestProvider { LanguageIds = { LanguageId.Csharp } },
            TestCaseDiscoveryProvider = new TestCaseDiscoveryProvider { LanguageIds = { LanguageId.Csharp } }
            // DebugProvider = new DebugProvider { LanguageIds = { LanguageId.Csharp } },
        };

        var initializeResult = new InitializeBuildResult
        {
            DisplayName = "dotnet-bsp",
            Version = "0.0.1",
            BspVersion = "2.1.1",
            Capabilities = serverCapabilities,
        };

        return initializeResult;
    }

    public InitializeBuildParams GetInitializeParams()
    {
        EnsureInitialize();

        return _initializeParams!;
    }

    public void EnsureInitialize()
    {
        _ = _initializeParams ?? throw new ServerNotInitializedException("Server not Initialized");
    }

    private bool _initialized = false;
    public bool Initialized
    {
        private get
        {
            return _initialized;
        }

        set
        {
            if (_initialized is true)
            {
                throw new InvalidOperationException("initialized was called twice");
            }

            _initialized = value;
        }
    }

    public void EnsureInitialized()
    {
        EnsureInitialize();

        if (!Initialized)
        {
            throw new ServerNotInitializedException("Client did not send Initialized notification");
        }
    }
}