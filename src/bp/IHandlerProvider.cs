using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace BaseProtocol;

/// <summary>
/// Manages handler discovery and distribution.
/// </summary>
public interface IHandlerProvider
{
    ImmutableArray<RequestHandlerMetadata> GetRegisteredMethods();

    IMethodHandler GetMethodHandler(string method, Type? requestType, Type? responseType);
}
