using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PageWeaver;

/// <summary>Capability-scoped links that let people without an account view, comment on, or approve a
/// document. Requires a <c>review</c>-scoped key.</summary>
public sealed class ShareLinksResource
{
    private readonly PageWeaverHttp _http;

    internal ShareLinksResource(PageWeaverHttp http) => _http = http;

    /// <summary>POST /v1/share-links. Create a share link. The response includes the raw url + token
    /// exactly once, so capture it now.</summary>
    public Task<Dictionary<string, JsonElement>> CreateAsync(object body, CancellationToken ct = default)
        => _http.RequestJsonAsync(HttpMethod.Post, "/v1/share-links", body, null, ct);

    /// <summary>GET /v1/share-links. List active + disabled links (never the tokens).</summary>
    public Task<Dictionary<string, JsonElement>> ListAsync(
        string? documentId = null, string? reviewRequestId = null, CancellationToken ct = default)
    {
        var query = PageWeaverHttp.BuildQuery(
            ("documentId", documentId),
            ("reviewRequestId", reviewRequestId));
        return _http.RequestJsonAsync(HttpMethod.Get, "/v1/share-links" + query, null, null, ct);
    }

    /// <summary>POST /v1/share-links/{id}/disable. Disable a link immediately.</summary>
    public Task<Dictionary<string, JsonElement>> DisableAsync(string id, CancellationToken ct = default)
        => _http.RequestJsonAsync(
            HttpMethod.Post, $"/v1/share-links/{PageWeaverHttp.Enc(id)}/disable", null, null, ct);
}
