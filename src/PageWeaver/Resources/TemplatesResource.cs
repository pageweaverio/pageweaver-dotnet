using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PageWeaver;

/// <summary>Read-only discovery of your published templates and their pinnable versions.</summary>
public sealed class TemplatesResource
{
    private readonly PageWeaverHttp _http;

    /// <summary>Template change proposals (the PR analog for template changes).</summary>
    public ProposalsResource Proposals { get; }

    internal TemplatesResource(PageWeaverHttp http)
    {
        _http = http;
        Proposals = new ProposalsResource(http);
    }

    /// <summary>GET /v1/templates. All templates owned by the key's account, newest-updated first.</summary>
    public Task<List<JsonElement>> ListAsync(CancellationToken ct = default)
        => _http.RequestJsonArrayAsync(HttpMethod.Get, "/v1/templates", null, null, ct);

    /// <summary>GET /v1/templates/{id}. One template's metadata.</summary>
    public Task<Dictionary<string, JsonElement>> GetAsync(string id, CancellationToken ct = default)
        => _http.RequestJsonAsync(HttpMethod.Get, $"/v1/templates/{PageWeaverHttp.Enc(id)}", null, null, ct);

    /// <summary>GET /v1/templates/{id}/versions. A template's published version history, newest first.</summary>
    public Task<List<JsonElement>> VersionsAsync(string id, CancellationToken ct = default)
        => _http.RequestJsonArrayAsync(
            HttpMethod.Get, $"/v1/templates/{PageWeaverHttp.Enc(id)}/versions", null, null, ct);

    /// <summary>GET /v1/templates/{id}/versions/{version}. One published version's metadata (pass
    /// <c>include="source"</c> for the frozen editor source).</summary>
    public Task<Dictionary<string, JsonElement>> VersionAsync(
        string id, int version, string? include = null, CancellationToken ct = default)
    {
        var query = PageWeaverHttp.BuildQuery(("include", include));
        return _http.RequestJsonAsync(
            HttpMethod.Get, $"/v1/templates/{PageWeaverHttp.Enc(id)}/versions/{version}" + query, null, null, ct);
    }
}
