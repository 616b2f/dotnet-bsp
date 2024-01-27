// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;
using BaseProtocol.Protocol;

namespace BaseProtocol;

/// <inheritdoc/>
public class HandlerProvider : IHandlerProvider
{
    private readonly IBpServices _bpServices;
    private ImmutableDictionary<RequestHandlerMetadata, Lazy<IMethodHandler>>? _requestHandlers;

    public HandlerProvider(IBpServices bpServices)
    {
        _bpServices = bpServices;
        RequiredMethods = new List<string>
        {
            Methods.Initialize,
            Methods.Initialized,
            Methods.Shutdown,
            Methods.Exit,
        };
    }


    public HandlerProvider(IBpServices bpServices, IReadOnlyList<string> requiredMethods)
    {
        _bpServices = bpServices;
        RequiredMethods = requiredMethods;
    }

    /// <summary>
    /// Get the MethodHandler for a particular request.
    /// </summary>
    /// <param name="method">The method name being made.</param>
    /// <param name="requestType">The requestType for this method.</param>
    /// <param name="responseType">The responseType for this method.</param>
    /// <returns>The handler for this request.</returns>
    public IMethodHandler GetMethodHandler(string method, Type? requestType, Type? responseType)
    {
        var requestHandlerMetadata = new RequestHandlerMetadata(method, requestType, responseType);

        var requestHandlers = GetRequestHandlers();
        if (!requestHandlers.TryGetValue(requestHandlerMetadata, out var lazyHandler))
        {
            throw new InvalidOperationException($"Missing handler for {requestHandlerMetadata.MethodName}");
        }

        return lazyHandler.Value;
    }

    public ImmutableArray<RequestHandlerMetadata> GetRegisteredMethods()
    {
        var requestHandlers = GetRequestHandlers();
        return requestHandlers.Keys.ToImmutableArray();
    }

    private ImmutableDictionary<RequestHandlerMetadata, Lazy<IMethodHandler>> GetRequestHandlers()
        => _requestHandlers ??= CreateMethodToHandlerMap(_bpServices);

    private ImmutableDictionary<RequestHandlerMetadata, Lazy<IMethodHandler>> CreateMethodToHandlerMap(IBpServices bpServices)
    {
        var requestHandlerDictionary = ImmutableDictionary.CreateBuilder<RequestHandlerMetadata, Lazy<IMethodHandler>>();

        var methodHash = new HashSet<string>();

        if (bpServices.SupportsGetRegisteredServices())
        {
            var requestHandlerTypes = bpServices.GetRegisteredServices().Where(type => typeof(IMethodHandler).IsAssignableFrom(type));

            foreach (var handlerType in requestHandlerTypes)
            {
                var requestResponseTypes = ConvertHandlerTypeToRequestResponseTypes(handlerType);
                foreach (var requestResponseType in requestResponseTypes)
                {
                    var method = GetRequestHandlerMethod(handlerType, requestResponseType.RequestType, requestResponseType.RequestContext, requestResponseType.ResponseType);

                    // Using the lazy set of handlers, create a lazy instance that will resolve the set of handlers for the provider
                    // and then lookup the correct handler for the specified method.

                    CheckForDuplicates(method, methodHash);

                    requestHandlerDictionary.Add(new RequestHandlerMetadata(method, requestResponseType.RequestType, requestResponseType.ResponseType), new Lazy<IMethodHandler>(() =>
                        {
                            var lspService = bpServices.TryGetService(handlerType);
                            if (lspService is null)
                            {
                                throw new InvalidOperationException($"{handlerType} could not be retrieved from service");
                            }

                            return (IMethodHandler)lspService;
                        }));
                }
            }
        }

        var handlers = bpServices.GetRequiredServices<IMethodHandler>();

        foreach (var handler in handlers)
        {
            var handlerType = handler.GetType();
            var requestResponseTypes = ConvertHandlerTypeToRequestResponseTypes(handlerType);
            foreach (var requestResponseType in requestResponseTypes)
            {
                var method = GetRequestHandlerMethod(handlerType, requestResponseType.RequestType, requestResponseType.RequestContext, requestResponseType.ResponseType);
                CheckForDuplicates(method, methodHash);

                requestHandlerDictionary.Add(new RequestHandlerMetadata(method, requestResponseType.RequestType, requestResponseType.ResponseType), new Lazy<IMethodHandler>(() => handler));
            }
        }

        VerifyHandlers(requestHandlerDictionary.Keys);

        return requestHandlerDictionary.ToImmutable();

        static void CheckForDuplicates(string methodName, HashSet<string> existingMethods)
        {
            if (!existingMethods.Add(methodName))
            {
                throw new InvalidOperationException($"Method {methodName} was implemented more than once.");
            }
        }

        static string GetRequestHandlerMethod(Type handlerType, Type? requestType, Type contextType, Type? responseType)
        {
            // Get the Protocol method name from the handler's method name attribute.
            var methodAttribute = GetMethodAttributeFromClassOrInterface(handlerType);
            if (methodAttribute is null)
            {
                methodAttribute = GetMethodAttributeFromHandlerMethod(handlerType, requestType, contextType, responseType);

                if (methodAttribute is null)
                {
                    throw new InvalidOperationException($"{handlerType.FullName} is missing {nameof(BaseProtocolServerEndpointAttribute)}");
                }
            }

            return methodAttribute.Method;

            static BaseProtocolServerEndpointAttribute? GetMethodAttributeFromHandlerMethod(Type handlerType, Type? requestType, Type contextType, Type? responseType)
            {
                const string handleRequestName = nameof(IRequestHandler<object, object, object>.HandleRequestAsync);
                const string handleNotificationName = nameof(INotificationHandler<object, object>.HandleNotificationAsync);

                foreach (var methodInfo in handlerType.GetRuntimeMethods())
                {
                    if (MethodInfoMatches(methodInfo))
                        return methodInfo.GetCustomAttribute<BaseProtocolServerEndpointAttribute>();
                }

                throw new InvalidOperationException("Somehow we are missing the method for our registered handler");

                bool MethodInfoMatches(MethodInfo methodInfo)
                {
                    switch (requestType != null, responseType != null)
                    {
                        case (true, true):
                            return (methodInfo.Name == handleRequestName || methodInfo.Name.EndsWith("." + handleRequestName)) &&
                                TypesMatch(methodInfo, [requestType!, contextType, typeof(CancellationToken)]);
                        case (false, true):
                            return (methodInfo.Name == handleRequestName || methodInfo.Name.EndsWith("." + handleRequestName)) &&
                                TypesMatch(methodInfo, [contextType, typeof(CancellationToken)]);
                        case (true, false):
                            return (methodInfo.Name == handleNotificationName || methodInfo.Name.EndsWith("." + handleNotificationName)) &&
                                TypesMatch(methodInfo, [requestType!, contextType, typeof(CancellationToken)]);
                        case (false, false):
                            return (methodInfo.Name == handleNotificationName || methodInfo.Name.EndsWith("." + handleNotificationName)) &&
                                TypesMatch(methodInfo, [contextType, typeof(CancellationToken)]);
                    }
                }

                bool TypesMatch(MethodInfo methodInfo, Type[] types)
                {
                    var parameters = methodInfo.GetParameters();
                    if (parameters.Length != types.Length)
                        return false;

                    for (int i = 0, n = parameters.Length; i < n; i++)
                    {
                        if (!Equals(types[i], parameters[i].ParameterType))
                            return false;
                    }

                    return true;
                }
            }

            static BaseProtocolServerEndpointAttribute? GetMethodAttributeFromClassOrInterface(Type type)
            {
                var attribute = Attribute.GetCustomAttribute(type, typeof(BaseProtocolServerEndpointAttribute)) as BaseProtocolServerEndpointAttribute;
                if (attribute is null)
                {
                    var interfaces = type.GetInterfaces();
                    foreach (var @interface in interfaces)
                    {
                        attribute = GetMethodAttributeFromClassOrInterface(@interface);
                        if (attribute is not null)
                        {
                            break;
                        }
                    }
                }

                return attribute;
            }
        }

        void VerifyHandlers(IEnumerable<RequestHandlerMetadata> requestHandlerKeys)
        {
            var missingMethods = requestHandlerKeys.Where(meta => RequiredMethods.All(method => method == meta.MethodName));
            if (missingMethods.Any())
            {
                throw new InvalidOperationException($"Base Server implementation is missing required methods {string.Join(",", missingMethods)}");
            }
        }
    }

    private readonly IReadOnlyList<string> RequiredMethods;

    private record HandlerTypes(Type? RequestType, Type? ResponseType, Type RequestContext);

    /// <summary>
    /// Retrieves the generic argument information from the request handler type without instantiating it.
    /// </summary>
    private static List<HandlerTypes> ConvertHandlerTypeToRequestResponseTypes(Type handlerType)
    {
        var handlerList = new List<HandlerTypes>();

        foreach (var interfaceType in handlerType.GetInterfaces())
        {
            if (!interfaceType.IsGenericType)
            {
                continue;
            }

            var genericDefinition = interfaceType.GetGenericTypeDefinition();

            HandlerTypes types;
            if (genericDefinition == typeof(IRequestHandler<,,>))
            {
                var genericArguments = interfaceType.GetGenericArguments();
                types = new HandlerTypes(RequestType: genericArguments[0], ResponseType: genericArguments[1], RequestContext: genericArguments[2]);
            }
            else if (genericDefinition == typeof(IRequestHandler<,>))
            {
                var genericArguments = interfaceType.GetGenericArguments();
                types = new HandlerTypes(RequestType: null, ResponseType: genericArguments[0], RequestContext: genericArguments[1]);
            }
            else if (genericDefinition == typeof(INotificationHandler<,>))
            {
                var genericArguments = interfaceType.GetGenericArguments();
                types = new HandlerTypes(RequestType: genericArguments[0], ResponseType: null, RequestContext: genericArguments[1]);
            }
            else if (genericDefinition == typeof(INotificationHandler<>))
            {
                var genericArguments = interfaceType.GetGenericArguments();
                types = new HandlerTypes(RequestType: null, ResponseType: null, RequestContext: genericArguments[0]);
            }
            else
            {
                continue;
            }

            handlerList.Add(types);
        }

        if (handlerList.Count == 0)
        {
            throw new InvalidOperationException($"Provided handler type {handlerType.FullName} does not implement {nameof(IMethodHandler)}");
        }

        return handlerList;
    }
}
