using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PageWeaver;

/// <summary>Review requests on documents: create, list, add participants, and collect approvals against
/// a completion policy. Requires a <c>review</c>-scoped key for writes.</summary>
public sealed class ReviewsResource
{
    private readonly PageWeaverHttp _http;

    internal ReviewsResource(PageWeaverHttp http) => _http = http;

    /// <summary>POST /v1/reviews. Open a review on a document with an optional policy + participants.</summary>
    public Task<Dictionary<string, JsonElement>> CreateAsync(object body, CancellationToken ct = default)
        => _http.RequestJsonAsync(HttpMethod.Post, "/v1/reviews", body, null, ct);

    /// <summary>GET /v1/reviews. List reviews, newest first.</summary>
    public Task<Dictionary<string, JsonElement>> ListAsync(
        string? status = null,
        string? documentId = null,
        string? cursor = null,
        int? limit = null,
        CancellationToken ct = default)
    {
        var query = PageWeaverHttp.BuildQuery(
            ("status", status),
            ("documentId", documentId),
            ("cursor", cursor),
            ("limit", limit));
        return _http.RequestJsonAsync(HttpMethod.Get, "/v1/reviews" + query, null, null, ct);
    }

    /// <summary>GET /v1/reviews/{id}. Fetch one review with participants, approvals, and policy state.</summary>
    public Task<Dictionary<string, JsonElement>> GetAsync(string id, CancellationToken ct = default)
        => _http.RequestJsonAsync(HttpMethod.Get, $"/v1/reviews/{PageWeaverHttp.Enc(id)}", null, null, ct);

    /// <summary>POST /v1/reviews/{id}/participants. Add a participant with a role.</summary>
    public Task<Dictionary<string, JsonElement>> AddParticipantAsync(
        string id, object body, CancellationToken ct = default)
        => _http.RequestJsonAsync(
            HttpMethod.Post, $"/v1/reviews/{PageWeaverHttp.Enc(id)}/participants", body, null, ct);

    /// <summary>POST /v1/reviews/{id}/approvals. Record an approval decision.</summary>
    public Task<Dictionary<string, JsonElement>> ApproveAsync(string id, object body, CancellationToken ct = default)
        => _http.RequestJsonAsync(HttpMethod.Post, $"/v1/reviews/{PageWeaverHttp.Enc(id)}/approvals", body, null, ct);

    /// <summary>POST /v1/reviews/{id}/complete. Manually complete a review.</summary>
    public Task<Dictionary<string, JsonElement>> CompleteAsync(string id, CancellationToken ct = default)
        => _http.RequestJsonAsync(HttpMethod.Post, $"/v1/reviews/{PageWeaverHttp.Enc(id)}/complete", null, null, ct);

    /// <summary>POST /v1/reviews/{id}/cancel. Withdraw a review (open → canceled).</summary>
    public Task<Dictionary<string, JsonElement>> CancelAsync(string id, CancellationToken ct = default)
        => _http.RequestJsonAsync(HttpMethod.Post, $"/v1/reviews/{PageWeaverHttp.Enc(id)}/cancel", null, null, ct);
}
