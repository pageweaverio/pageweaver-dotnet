using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PageWeaver;

/// <summary>Read-only discovery of the JSON Schemas your payloads validate against.</summary>
public sealed class SchemasResource
{
    private readonly PageWeaverHttp _http;

    internal SchemasResource(PageWeaverHttp http) => _http = http;

    /// <summary>GET /v1/schemas. All schemas owned by the key's account, newest-updated first.</summary>
    public Task<List<JsonElement>> ListAsync(CancellationToken ct = default)
        => _http.RequestJsonArrayAsync(HttpMethod.Get, "/v1/schemas", null, null, ct);

    /// <summary>GET /v1/schemas/{id}. A schema's published JSON Schema plus a derived sample; pass a
    /// <paramref name="version"/> to target a specific published version.</summary>
    public Task<Dictionary<string, JsonElement>> GetAsync(
        string id, int? version = null, CancellationToken ct = default)
    {
        var query = PageWeaverHttp.BuildQuery(("version", version));
        return _http.RequestJsonAsync(HttpMethod.Get, $"/v1/schemas/{PageWeaverHttp.Enc(id)}" + query, null, null, ct);
    }

    /// <summary>GET /v1/schemas/{id}/versions. A schema's published version history, newest first.</summary>
    public Task<List<JsonElement>> VersionsAsync(string id, CancellationToken ct = default)
        => _http.RequestJsonArrayAsync(
            HttpMethod.Get, $"/v1/schemas/{PageWeaverHttp.Enc(id)}/versions", null, null, ct);

    /// <summary>GET /v1/schemas/{id}/versions/{version}. One published version's metadata (pass
    /// <c>include="nodes"</c> for the frozen FieldNode tree).</summary>
    public Task<Dictionary<string, JsonElement>> VersionAsync(
        string id, int version, string? include = null, CancellationToken ct = default)
    {
        var query = PageWeaverHttp.BuildQuery(("include", include));
        return _http.RequestJsonAsync(
            HttpMethod.Get, $"/v1/schemas/{PageWeaverHttp.Enc(id)}/versions/{version}" + query, null, null, ct);
    }
}
