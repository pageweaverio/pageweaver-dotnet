using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PageWeaver;

/// <summary>Client for the PageWeaver document-generation API.</summary>
public sealed class PageWeaverClient
{
    private const string DefaultBaseUrl = "https://api.pageweaver.io";
    private static readonly HashSet<string> Terminal = new() { "done", "failed", "error" };

    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _baseUrl;

    public PageWeaverClient(string apiKey, string baseUrl = DefaultBaseUrl, HttpClient? httpClient = null)
    {
        if (string.IsNullOrEmpty(apiKey))
            throw new ArgumentException("apiKey is required", nameof(apiKey));
        _apiKey = apiKey;
        _baseUrl = baseUrl.TrimEnd('/');
        _http = httpClient ?? new HttpClient();
    }

    /// <summary>POST /v1/documents. Pass the request body per the API docs.</summary>
    public Task<Dictionary<string, JsonElement>> CreateDocumentAsync(object body, CancellationToken ct = default)
        => RequestAsync(HttpMethod.Post, "/v1/documents", body, ct);

    /// <summary>GET /v1/documents/:id.</summary>
    public Task<Dictionary<string, JsonElement>> GetDocumentAsync(string id, CancellationToken ct = default)
        => RequestAsync(HttpMethod.Get, "/v1/documents/" + Uri.EscapeDataString(id), null, ct);

    /// <summary>Create a document and poll until it reaches a terminal state.</summary>
    public async Task<Dictionary<string, JsonElement>> CreateAndWaitAsync(
        object body,
        TimeSpan? pollInterval = null,
        TimeSpan? timeout = null,
        CancellationToken ct = default)
    {
        var interval = pollInterval ?? TimeSpan.FromSeconds(1);
        var limit = timeout ?? TimeSpan.FromSeconds(60);
        var created = await CreateDocumentAsync(body, ct).ConfigureAwait(false);
        if (!created.TryGetValue("id", out var idEl) || idEl.ValueKind != JsonValueKind.String)
            return created;

        var id = idEl.GetString()!;
        var deadline = DateTime.UtcNow + limit;
        while (true)
        {
            var doc = await GetDocumentAsync(id, ct).ConfigureAwait(false);
            if (doc.TryGetValue("status", out var st)
                && st.ValueKind == JsonValueKind.String
                && Terminal.Contains(st.GetString()!))
            {
                return doc;
            }
            if (DateTime.UtcNow >= deadline)
                throw new PageWeaverException($"Timed out waiting for document {id}");
            await Task.Delay(interval, ct).ConfigureAwait(false);
        }
    }

    private async Task<Dictionary<string, JsonElement>> RequestAsync(
        HttpMethod method, string path, object? body, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(method, _baseUrl + path);
        req.Headers.TryAddWithoutValidation("x-api-key", _apiKey);
        req.Headers.TryAddWithoutValidation("Accept", "application/json");
        if (body is not null)
            req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        using var resp = await _http.SendAsync(req, ct).ConfigureAwait(false);
        var raw = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

        if (!resp.IsSuccessStatusCode)
            throw new PageWeaverException(
                $"{method} {path} failed with status {(int)resp.StatusCode}", (int)resp.StatusCode, raw);

        if (string.IsNullOrEmpty(raw))
            return new Dictionary<string, JsonElement>();

        return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(raw)
               ?? new Dictionary<string, JsonElement>();
    }
}
