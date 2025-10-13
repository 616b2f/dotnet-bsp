using BaseProtocol;
using bsp4csharp.Protocol;
using Microsoft.Build.Evaluation;

namespace dotnet_bsp.Handlers;

[BaseProtocolServerEndpoint(Methods.BuildTargetSources)]
internal class BuildTargetSourcesHandler
    : IRequestHandler<SourcesParams, SourcesResult, RequestContext>
{
    private readonly BuildInitializeManager _initializeManager;

    public BuildTargetSourcesHandler(BuildInitializeManager initializeManager)
    {
        _initializeManager = initializeManager;
    }

    public bool MutatesSolutionState => false;

    public Task<SourcesResult> HandleRequestAsync(SourcesParams sourcesParams, RequestContext context, CancellationToken cancellationToken)
    {
        _initializeManager.EnsureInitialized();

        var items = new List<SourcesItem>();
        // TODO: handling for .sln targets is not implemented
        foreach (var target in sourcesParams.Targets)
        {
            if (target.ToString().EndsWith(".csproj"))
            {
                var pcol = new ProjectCollection();
                var proj = pcol.LoadProject(target.ToString());
                var documents = proj.GetItems("Compile");

                var rootDir = Path.GetDirectoryName(target.ToString());

                var sources = new List<SourceItem>();
                foreach (var document in documents)
                {
                    var path = Path.GetFullPath(Path.Combine("./", document.EvaluatedInclude), rootDir ?? "/");
                    context.Logger.LogInformation("Document: {0}", path);
                    sources.Add(new SourceItem
                    {
                        Uri = UriFixer.WithFileSchema(path),
                        Kind = SourceItemKind.File,
                        Generated = false
                    });
                }

                Uri[] roots = (rootDir != null) ? [UriFixer.WithFileSchema(rootDir)] : [];
                var sourcesItem = new SourcesItem
                {
                    Target = target,
                    Sources = sources.ToArray(),
                    Roots = roots
                };
                items.Add(sourcesItem);
            }
        }
        var result = new SourcesResult
        {
            Items = items.ToArray()
        };
        return Task.FromResult(result);
    }
}