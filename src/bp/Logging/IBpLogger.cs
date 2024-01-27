﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace BaseProtocol;

public interface IBpLogger
{
    void LogStartContext(string message, params object[] @params);
    void LogEndContext(string message, params object[] @params);
    void LogInformation(string message, params object[] @params);
    void LogWarning(string message, params object[] @params);
    void LogError(string message, params object[] @params);
    void LogException(Exception exception, string? message = null, params object[] @params);
}
