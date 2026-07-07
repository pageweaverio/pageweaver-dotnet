using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PageWeaver;

/// <summary>Your page consumption against the plan quota for the current billing period.</summary>
public sealed class UsageResource
{
    private readonly PageWeaverHttp _http;

    internal UsageResource(PageWeaverHttp http) => _http = http;

    /// <summary>GET /v1/usage. Current-period usage: billable document pages and editor preview pages,
    /// with their limits.</summary>
    public Task<Dictionary<string, JsonElement>> GetAsync(CancellationToken ct = default)
        => _http.RequestJsonAsync(HttpMethod.Get, "/v1/usage", null, null, ct);
}
