using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PageWeaver;

/// <summary>Environments and pins: a named per-account pointer set over immutable template versions, so
/// a create with <c>environment: "production"</c> renders the pinned version. Writes require a
/// <c>deploy</c>-scoped key.</summary>
public sealed class EnvironmentsResource
{
    private readonly PageWeaverHttp _http;

    internal EnvironmentsResource(PageWeaverHttp http) => _http = http;

    /// <summary>GET /v1/environments. Every environment for the account, with pin counts.</summary>
    public Task<List<JsonElement>> ListAsync(CancellationToken ct = default)
        => _http.RequestJsonArrayAsync(HttpMethod.Get, "/v1/environments", null, null, ct);

    /// <summary>POST /v1/environments. Create a named pointer set (e.g. staging / production).</summary>
    public Task<Dictionary<string, JsonElement>> CreateAsync(object body, CancellationToken ct = default)
        => _http.RequestJsonAsync(HttpMethod.Post, "/v1/environments", body, null, ct);

    /// <summary>GET /v1/environments/{slug}. Fetch one environment by slug.</summary>
    public Task<Dictionary<string, JsonElement>> GetAsync(string slug, CancellationToken ct = default)
        => _http.RequestJsonAsync(HttpMethod.Get, $"/v1/environments/{PageWeaverHttp.Enc(slug)}", null, null, ct);

    /// <summary>PATCH /v1/environments/{slug}. Rename an environment or flip its production flag.</summary>
    public Task<Dictionary<string, JsonElement>> UpdateAsync(
        string slug, object body, CancellationToken ct = default)
        => _http.RequestJsonAsync(HttpMethod.Patch, $"/v1/environments/{PageWeaverHttp.Enc(slug)}", body, null, ct);

    /// <summary>DELETE /v1/environments/{slug}. Delete an environment and its pins.</summary>
    public Task<Dictionary<string, JsonElement>> DeleteAsync(string slug, CancellationToken ct = default)
        => _http.RequestJsonAsync(HttpMethod.Delete, $"/v1/environments/{PageWeaverHttp.Enc(slug)}", null, null, ct);

    /// <summary>GET /v1/environments/{slug}/pins. The template → version pointers in an environment.</summary>
    public Task<List<JsonElement>> PinsAsync(string slug, CancellationToken ct = default)
        => _http.RequestJsonArrayAsync(
            HttpMethod.Get, $"/v1/environments/{PageWeaverHttp.Enc(slug)}/pins", null, null, ct);

    /// <summary>PUT /v1/environments/{slug}/pins/{templateId}. Point a template at a published version.</summary>
    public Task<Dictionary<string, JsonElement>> SetPinAsync(
        string slug, string templateId, int version, CancellationToken ct = default)
        => _http.RequestJsonAsync(
            HttpMethod.Put,
            $"/v1/environments/{PageWeaverHttp.Enc(slug)}/pins/{PageWeaverHttp.Enc(templateId)}",
            new { version },
            null, ct);

    /// <summary>DELETE /v1/environments/{slug}/pins/{templateId}. Unpin a template from an environment.</summary>
    public Task<Dictionary<string, JsonElement>> RemovePinAsync(
        string slug, string templateId, CancellationToken ct = default)
        => _http.RequestJsonAsync(
            HttpMethod.Delete,
            $"/v1/environments/{PageWeaverHttp.Enc(slug)}/pins/{PageWeaverHttp.Enc(templateId)}",
            null, null, ct);

    /// <summary>POST /v1/environments/{slug}/promote. Copy another environment's pin set onto this one.</summary>
    public Task<Dictionary<string, JsonElement>> PromoteAsync(
        string slug, object body, CancellationToken ct = default)
        => _http.RequestJsonAsync(
            HttpMethod.Post, $"/v1/environments/{PageWeaverHttp.Enc(slug)}/promote", body, null, ct);

    /// <summary>POST /v1/environments/{slug}/rollback. Roll an environment back to a prior pin set.</summary>
    public Task<Dictionary<string, JsonElement>> RollbackAsync(
        string slug, object? body = null, CancellationToken ct = default)
        => _http.RequestJsonAsync(
            HttpMethod.Post, $"/v1/environments/{PageWeaverHttp.Enc(slug)}/rollback", body, null, ct);
}
