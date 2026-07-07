using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PageWeaver;

/// <summary>Anchored comment threads on rendered documents: create, list, reply, and lifecycle
/// (resolve / reopen / close). Requires a <c>review</c>-scoped key for writes.</summary>
public sealed class CommentsResource
{
    private readonly PageWeaverHttp _http;

    internal CommentsResource(PageWeaverHttp http) => _http = http;

    /// <summary>POST /v1/comments. Create an anchored thread with its first message.</summary>
    public Task<Dictionary<string, JsonElement>> CreateAsync(object body, CancellationToken ct = default)
        => _http.RequestJsonAsync(HttpMethod.Post, "/v1/comments", body, null, ct);

    /// <summary>GET /v1/documents/{documentId}/comments. List a document's threads, newest first.</summary>
    public Task<Dictionary<string, JsonElement>> ListAsync(
        string documentId,
        int? pageNumber = null,
        string? status = null,
        string? severity = null,
        string? cursor = null,
        int? limit = null,
        CancellationToken ct = default)
    {
        var query = PageWeaverHttp.BuildQuery(
            ("pageNumber", pageNumber),
            ("status", status),
            ("severity", severity),
            ("cursor", cursor),
            ("limit", limit));
        return _http.RequestJsonAsync(
            HttpMethod.Get, $"/v1/documents/{PageWeaverHttp.Enc(documentId)}/comments" + query, null, null, ct);
    }

    /// <summary>GET /v1/comments/{id}. Fetch one thread with its full message list.</summary>
    public Task<Dictionary<string, JsonElement>> GetAsync(string id, CancellationToken ct = default)
        => _http.RequestJsonAsync(HttpMethod.Get, $"/v1/comments/{PageWeaverHttp.Enc(id)}", null, null, ct);

    /// <summary>PATCH /v1/comments/{id}. Edit severity, assignment, due date, or relocate the anchor.</summary>
    public Task<Dictionary<string, JsonElement>> UpdateAsync(string id, object body, CancellationToken ct = default)
        => _http.RequestJsonAsync(HttpMethod.Patch, $"/v1/comments/{PageWeaverHttp.Enc(id)}", body, null, ct);

    /// <summary>POST /v1/comments/{id}/messages. Reply on a thread.</summary>
    public Task<Dictionary<string, JsonElement>> ReplyAsync(string id, object body, CancellationToken ct = default)
        => _http.RequestJsonAsync(HttpMethod.Post, $"/v1/comments/{PageWeaverHttp.Enc(id)}/messages", body, null, ct);

    /// <summary>POST /v1/comments/{id}/resolve. Resolve a thread (open → resolved).</summary>
    public Task<Dictionary<string, JsonElement>> ResolveAsync(string id, CancellationToken ct = default)
        => _http.RequestJsonAsync(HttpMethod.Post, $"/v1/comments/{PageWeaverHttp.Enc(id)}/resolve", null, null, ct);

    /// <summary>POST /v1/comments/{id}/reopen. Reopen a resolved thread (resolved → open).</summary>
    public Task<Dictionary<string, JsonElement>> ReopenAsync(string id, CancellationToken ct = default)
        => _http.RequestJsonAsync(HttpMethod.Post, $"/v1/comments/{PageWeaverHttp.Enc(id)}/reopen", null, null, ct);

    /// <summary>POST /v1/comments/{id}/close. Close a thread permanently (→ closed, final).</summary>
    public Task<Dictionary<string, JsonElement>> CloseAsync(string id, CancellationToken ct = default)
        => _http.RequestJsonAsync(HttpMethod.Post, $"/v1/comments/{PageWeaverHttp.Enc(id)}/close", null, null, ct);
}
