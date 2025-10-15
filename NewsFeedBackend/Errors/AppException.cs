namespace NewsFeedBackend.Errors;

public abstract class AppException : Exception
{
    public int StatusCode { get; }
    public string? Code { get; }

    protected AppException(string message, int statusCode, string? code = null, Exception? inner = null)
        : base(message, inner)
    {
        StatusCode = statusCode;
        Code = code;
    }
}
