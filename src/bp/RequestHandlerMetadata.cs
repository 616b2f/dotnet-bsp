using System;

namespace BaseProtocol;

public record RequestHandlerMetadata(string MethodName, Type? RequestType, Type? ResponseType);
