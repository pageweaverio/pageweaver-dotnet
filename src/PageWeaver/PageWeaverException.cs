using System;
using System.Collections.Generic;
using System.Text.Json;

namespace PageWeaver;

/// <summary>Base class for every error the SDK throws. Non-2xx API responses raise this
/// (or a subclass); it carries the HTTP status and raw response body when available.</summary>
public class PageWeaverException : Exception
{
    /// <summary>The HTTP status code of the failing response, when the error came from the API.</summary>
    public int? StatusCode { get; }

    /// <summary>The raw response body, when the error came from the API.</summary>
    public string? Body { get; }

    public PageWeaverException(string message, int? statusCode = null, string? body = null)
        : base(message)
    {
        StatusCode = statusCode;
        Body = body;
    }
}

/// <summary><c>WaitForAsync</c>/<c>CreateAndWaitAsync</c> exceeded its timeout before the document
/// reached a terminal state.</summary>
public sealed class PageWeaverTimeoutException : PageWeaverException
{
    /// <summary>The id of the document being waited on.</summary>
    public string DocumentId { get; }

    /// <summary>The last observed status before the timeout elapsed.</summary>
    public string? LastStatus { get; }

    public PageWeaverTimeoutException(string documentId, string? lastStatus, double timeoutMs)
        : base($"Timed out after {timeoutMs}ms waiting for document {documentId} (last status: {lastStatus}).")
    {
        DocumentId = documentId;
        LastStatus = lastStatus;
    }
}

/// <summary>The document reached the terminal <c>failed</c> state while waiting (or a download was
/// requested for a document that is not downloadable). <see cref="Document"/> carries the final
/// response, including any <c>error</c> string.</summary>
public sealed class PageWeaverDocumentFailedException : PageWeaverException
{
    /// <summary>The final document response.</summary>
    public IReadOnlyDictionary<string, JsonElement> Document { get; }

    public PageWeaverDocumentFailedException(IReadOnlyDictionary<string, JsonElement> document)
        : base(BuildMessage(document))
    {
        Document = document;
    }

    private static string BuildMessage(IReadOnlyDictionary<string, JsonElement> document)
    {
        var id = document.TryGetValue("id", out var idEl) && idEl.ValueKind == JsonValueKind.String
            ? idEl.GetString()
            : "unknown";
        var error = document.TryGetValue("error", out var errEl) && errEl.ValueKind == JsonValueKind.String
            ? errEl.GetString()
            : "unknown error";
        return $"Document {id} failed: {error}";
    }
}

/// <summary>Thrown when a webhook signature does not match the request body.</summary>
public sealed class PageWeaverWebhookSignatureException : PageWeaverException
{
    public PageWeaverWebhookSignatureException()
        : base("Invalid webhook signature.")
    {
    }
}
