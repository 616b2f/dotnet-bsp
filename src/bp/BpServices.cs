using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.Extensions.DependencyInjection;

namespace BaseProtocol;

public class BpServices : IBpServices
{
    private readonly IServiceProvider _serviceProvider;

    public BpServices(IServiceCollection serviceCollection)
    {
        _ = serviceCollection.AddSingleton<IBpServices>(this);
        _serviceProvider = serviceCollection.BuildServiceProvider();
    }

    public T GetRequiredService<T>() where T : notnull
    {
        var service = _serviceProvider.GetRequiredService<T>();

        return service;
    }

    public object? TryGetService(Type type)
    {
        var obj = _serviceProvider.GetService(type);

        return obj;
    }

    public IEnumerable<TService> GetServices<TService>()
    {
        return _serviceProvider.GetServices<TService>();
    }

    public void Dispose()
    {
    }

    public IEnumerable<T> GetRequiredServices<T>()
    {
        var services = _serviceProvider.GetServices<T>();

        return services;
    }

    public ImmutableArray<Type> GetRegisteredServices()
    {
        throw new NotImplementedException();
    }

    public bool SupportsGetRegisteredServices()
    {
        return false;
    }
}

