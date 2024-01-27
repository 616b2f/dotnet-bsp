// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace BaseProtocol;

/// <summary>
/// An attribute which identifies the method which an <see cref="IMethodHandler"/> implements.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = false)]
public class BaseProtocolServerEndpointAttribute : Attribute
{
    /// <summary>
    /// Contains the method that this <see cref="IMethodHandler"/> implements.
    /// </summary>
    public string Method { get; }

    public BaseProtocolServerEndpointAttribute(string method)
    {
        Method = method;
    }
}
