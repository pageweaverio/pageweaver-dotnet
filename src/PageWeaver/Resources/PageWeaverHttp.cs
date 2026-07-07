using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PageWeaver;

/// <summary>Internal transport shared by every resource: holds the <see cref="HttpClient"/>, the API
/// key, and the base URL, and centralizes request building, JSON (de)serialization, error mapping, and
/// raw-byte / raw-response access.</summary>
public sealed class PageWeaverHttp
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _baseUrl;

    public PageWeaverHttp(string apiKey, string baseUrl, HttpClient? httpClient)
    {
        if (string.IsNullOrEmpty(apiKey))
            throw new ArgumentException("apiKey is required", nameof(apiKey));
        _apiKey = apiKey;
        _baseUrl = baseUrl.TrimEnd('/');
        _http = httpClient ?? new HttpClient();
    }

    /// <summary>Build a query string from the given key/value pairs, omitting null or empty values.</summary>
    public static string BuildQuery(params (string Key, object? Value)[] pairs)
    {
        var parts = new List<string>();
        foreach (var (key, value) in pairs)
        {
            if (value is null) continue;
            var s = value is IFormattable f
                ? f.ToString(null, System.Globalization.CultureInfo.InvariantCulture)
                : value.ToString();
            if (string.IsNullOrEmpty(s)) continue;
            parts.Add($"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(s)}");
        }
        return parts.Count == 0 ? string.Empty : "?" + string.Join("&", parts);
    }

    /// <summary>Escape a path segment (id).</summary>
    public static string Enc(string value) => Uri.EscapeDataString(value);

    /// <summary>Perform a request and deserialize a JSON object response into a dictionary.</summary>
    public async Task<Dictionary<string, JsonElement>> RequestJsonAsync(
        HttpMethod method,
        string path,
        object? body = null,
        IReadOnlyDictionary<string, string>? headers = null,
        CancellationToken ct = default)
    {
        using var resp = await SendAsync(method, path, body, headers, noAuth: false, accept: "application/json", ct)
            .ConfigureAwait(false);
        var raw = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        if (string.IsNullOrEmpty(raw))
            return new Dictionary<string, JsonElement>();
        return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(raw)
               ?? new Dictionary<string, JsonElement>();
    }

    /// <summary>Perform a request and deserialize a JSON array response into a list.</summary>
    public async Task<List<JsonElement>> RequestJsonArrayAsync(
        HttpMethod method,
        string path,
        object? body = null,
        IReadOnlyDictionary<string, string>? headers = null,
        CancellationToken ct = default)
    {
        using var resp = await SendAsync(method, path, body, headers, noAuth: false, accept: "application/json", ct)
            .ConfigureAwait(false);
        var raw = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        if (string.IsNullOrEmpty(raw))
            return new List<JsonElement>();
        return JsonSerializer.Deserialize<List<JsonElement>>(raw) ?? new List<JsonElement>();
    }

    /// <summary>Perform a request and return the raw body bytes (for PDF downloads). Optionally skip the
    /// <c>x-api-key</c> header (the password-gated content endpoint is not API-key authenticated).</summary>
    public async Task<byte[]> RequestBytesAsync(
        HttpMethod method,
        string path,
        IReadOnlyDictionary<string, string>? headers = null,
        bool noAuth = false,
        CancellationToken ct = default)
    {
        using var resp = await SendAsync(method, path, body: null, headers, noAuth, accept: null, ct)
            .ConfigureAwait(false);
        return await resp.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Fetch an absolute URL (e.g. a signed download URL) with no authentication, returning its
    /// bytes.</summary>
    public async Task<byte[]> FetchUrlBytesAsync(string url, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        using var resp = await _http.SendAsync(req, ct).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode)
        {
            var raw = await SafeReadAsync(resp, ct).ConfigureAwait(false);
            throw new PageWeaverException(
                $"Failed to download from {url}: {(int)resp.StatusCode}", (int)resp.StatusCode, raw);
        }
        return await resp.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Perform a request and return the raw <see cref="HttpResponseMessage"/> (2xx only; non-2xx
    /// still throws). For content-negotiated endpoints whose body may be JSON or bytes. The caller owns
    /// disposing the response.</summary>
    public Task<HttpResponseMessage> RequestRawAsync(
        HttpMethod method,
        string path,
        object? body = null,
        IReadOnlyDictionary<string, string>? headers = null,
        string? accept = "application/json",
        CancellationToken ct = default)
        => SendAsync(method, path, body, headers, noAuth: false, accept, ct);

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string path,
        object? body,
        IReadOnlyDictionary<string, string>? headers,
        bool noAuth,
        string? accept,
        CancellationToken ct)
    {
        var req = new HttpRequestMessage(method, _baseUrl + path);
        if (!noAuth)
            req.Headers.TryAddWithoutValidation("x-api-key", _apiKey);
        if (accept is not null)
            req.Headers.TryAddWithoutValidation("Accept", accept);
        if (headers is not null)
        {
            foreach (var kvp in headers)
                req.Headers.TryAddWithoutValidation(kvp.Key, kvp.Value);
        }
        if (body is not null)
            req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        HttpResponseMessage resp;
        try
        {
            resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        }
        finally
        {
            req.Dispose();
        }

        if (!resp.IsSuccessStatusCode)
        {
            var raw = await SafeReadAsync(resp, ct).ConfigureAwait(false);
            var status = (int)resp.StatusCode;
            resp.Dispose();
            throw new PageWeaverException(ExtractMessage(raw, status), status, raw);
        }

        return resp;
    }

    private static async Task<string> SafeReadAsync(HttpResponseMessage resp, CancellationToken ct)
    {
        try
        {
            return await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>Pull a human-readable message out of an error body. <c>body.message</c> may be a string
    /// OR an array of strings; fall back to a generic message.</summary>
    private static string ExtractMessage(string raw, int status)
    {
        if (!string.IsNullOrEmpty(raw))
        {
            try
            {
                using var doc = JsonDocument.Parse(raw);
                if (doc.RootElement.ValueKind == JsonValueKind.Object
                    && doc.RootElement.TryGetProperty("message", out var msg))
                {
                    if (msg.ValueKind == JsonValueKind.String)
                    {
                        var s = msg.GetString();
                        if (!string.IsNullOrEmpty(s)) return s!;
                    }
                    else if (msg.ValueKind == JsonValueKind.Array)
                    {
                        var joined = string.Join(", ",
                            msg.EnumerateArray()
                               .Select(e => e.ValueKind == JsonValueKind.String ? e.GetString() : e.ToString())
                               .Where(x => !string.IsNullOrEmpty(x)));
                        if (!string.IsNullOrEmpty(joined)) return joined;
                    }
                }
            }
            catch (JsonException)
            {
                // Non-JSON body: fall through to the generic message.
            }
        }
        return $"Request failed with status {status}";
    }
}
