using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PageWeaver;

/// <summary>Deployments: documents-as-code. Plan a manifest against a target environment (the server
/// re-validates and diffs it), then apply it in a separate explicit call. Writes require a
/// <c>deploy</c>-scoped key.</summary>
public sealed class DeploymentsResource
{
    private readonly PageWeaverHttp _http;

    internal DeploymentsResource(PageWeaverHttp http) => _http = http;

    /// <summary>POST /v1/deployments/plan. Plan a deployment; nothing is applied.</summary>
    public Task<Dictionary<string, JsonElement>> PlanAsync(
        object body, string? idempotencyKey = null, CancellationToken ct = default)
    {
        var headers = idempotencyKey is null
            ? null
            : new Dictionary<string, string> { ["Idempotency-Key"] = idempotencyKey };
        return _http.RequestJsonAsync(HttpMethod.Post, "/v1/deployments/plan", body, headers, ct);
    }

    /// <summary>GET /v1/deployments. Recent deployments for the account, newest first.</summary>
    public Task<List<JsonElement>> ListAsync(
        string? environment = null, int? limit = null, CancellationToken ct = default)
    {
        var query = PageWeaverHttp.BuildQuery(("environment", environment), ("limit", limit));
        return _http.RequestJsonArrayAsync(HttpMethod.Get, "/v1/deployments" + query, null, null, ct);
    }

    /// <summary>GET /v1/deployments/{id}. One deployment with its per-resource plan lines and outcomes.</summary>
    public Task<Dictionary<string, JsonElement>> GetAsync(string id, CancellationToken ct = default)
        => _http.RequestJsonAsync(HttpMethod.Get, $"/v1/deployments/{PageWeaverHttp.Enc(id)}", null, null, ct);

    /// <summary>POST /v1/deployments/{id}/apply. Apply a planned deployment.</summary>
    public Task<Dictionary<string, JsonElement>> ApplyAsync(string id, CancellationToken ct = default)
        => _http.RequestJsonAsync(
            HttpMethod.Post, $"/v1/deployments/{PageWeaverHttp.Enc(id)}/apply", null, null, ct);
}
