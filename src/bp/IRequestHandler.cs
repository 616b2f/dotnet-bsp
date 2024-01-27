using System;
using System.Threading;
using System.Threading.Tasks;

namespace BaseProtocol;

public interface IRequestHandler<TRequest, TResponse, TRequestContext> : IMethodHandler
{
    /// <summary>
    /// Handles an BP request in the context of the supplied document and/or solution.
    /// </summary>
    /// <param name="request">The request parameters.</param>
    /// <param name="context">The LSP request context, which should have been filled in with document information from <see cref="ITextDocumentIdentifierHandler{RequestType, TextDocumentIdentifierType}.GetTextDocumentIdentifier(RequestType)"/> if applicable.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the request processing.</param>
    /// <returns>The LSP response.</returns>
    Task<TResponse> HandleRequestAsync(TRequest request, TRequestContext context, CancellationToken cancellationToken);
}

public interface IRequestHandler<TResponse, TRequestContext> : IMethodHandler
{
    /// <summary>
    /// Handles an BP request in the context of the supplied document and/or solution.
    /// </summary>
    /// <param name="context">The LSP request context, which should have been filled in with document information from <see cref="ITextDocumentIdentifierHandler{RequestType, TextDocumentIdentifierType}.GetTextDocumentIdentifier(RequestType)"/> if applicable.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the request processing.</param>
    /// <returns>The LSP response.</returns>
    Task<TResponse> HandleRequestAsync(TRequestContext context, CancellationToken cancellationToken);
}
