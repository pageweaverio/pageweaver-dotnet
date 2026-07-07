using System;

namespace PageWeaver;

/// <summary>Raised when the API returns a non-2xx response, or the request fails.</summary>
public sealed class PageWeaverException : Exception
{
    public int? StatusCode { get; }
    public string? Body { get; }

    public PageWeaverException(string message, int? statusCode = null, string? body = null)
        : base(message)
    {
        StatusCode = statusCode;
        Body = body;
    }
}
