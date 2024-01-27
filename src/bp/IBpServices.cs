using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace BaseProtocol;

public interface IBpServices : IDisposable
{
    T GetRequiredService<T>() where T : notnull;

    object? TryGetService(Type @type);

    IEnumerable<T> GetRequiredServices<T>();

    // TODO: https://github.com/dotnet/roslyn/issues/63555
    // These two methods should ideally be removed, but that would required
    // Roslyn to allow non-lazy creation of IMethodHandlers which they currently cannot
    ImmutableArray<Type> GetRegisteredServices();

    bool SupportsGetRegisteredServices();
}
