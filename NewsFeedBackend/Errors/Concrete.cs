namespace NewsFeedBackend.Errors;

public sealed class ValidationException : AppException
{
    public ValidationException(string message, string? code = null)
        : base(message, 400, code) { }
}

public sealed class UnauthorizedAppException : AppException
{
    public UnauthorizedAppException(string message = "Unauthorized", string? code = null)
        : base(message, 401, code) { }
}

public sealed class ForbiddenException : AppException
{
    public ForbiddenException(string message = "Forbidden", string? code = null)
        : base(message, 403, code) { }
}

public sealed class NotFoundException : AppException
{
    public NotFoundException(string message, string? code = null)
        : base(message, 404, code) { }
}

public sealed class ConflictException : AppException
{
    public ConflictException(string message, string? code = null)
        : base(message, 409, code) { }
}

public sealed class RateLimitException : AppException
{
    public RateLimitException(string message = "Rate limit exceeded", string? code = null)
        : base(message, 429, code) { }
}

public sealed class ExternalServiceException : AppException
{
    public ExternalServiceException(string message, string? code = null, Exception? inner = null)
        : base(message, 502, code, inner) { }
}
