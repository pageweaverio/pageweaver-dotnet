using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PageWeaver;

/// <summary>Options for <see cref="DocumentsResource.WaitForAsync"/> /
/// <see cref="DocumentsResource.CreateAndWaitAsync"/>.</summary>
public sealed class WaitOptions
{
    /// <summary>Initial delay between polls. Default 1s.</summary>
    public TimeSpan IntervalMs { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>Cap the (backing-off) poll delay. Default 60s.</summary>
    public TimeSpan MaxIntervalMs { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>Multiplier applied to the delay after each poll. Default 1.5.</summary>
    public double Backoff { get; set; } = 1.5;

    /// <summary>Give up after this long. Default 5min.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>Throw <see cref="PageWeaverDocumentFailedException"/> if the document fails. Default true.</summary>
    public bool ThrowOnFailure { get; set; } = true;
}

/// <summary>The result of a content-negotiated synchronous create
/// (<see cref="DocumentsResource.CreateSyncAsync"/>).</summary>
public sealed class SyncResult
{
    /// <summary>One of "pdf", "pending", or "document".</summary>
    public string Kind { get; init; } = "document";

    /// <summary>The document id (present for "pdf" and "pending").</summary>
    public string? Id { get; init; }

    /// <summary>The document version, when known.</summary>
    public int? Version { get; init; }

    /// <summary>Raw PDF bytes (present when <see cref="Kind"/> is "pdf").</summary>
    public byte[]? Pdf { get; init; }

    /// <summary>The document status (present when <see cref="Kind"/> is "pending").</summary>
    public string? Status { get; init; }

    /// <summary>The finished document body (present when <see cref="Kind"/> is "document").</summary>
    public Dictionary<string, JsonElement>? Document { get; init; }
}

/// <summary>Optional filters for <see cref="DocumentsResource.ListAsync(DocumentListParams,CancellationToken)"/>.</summary>
public sealed class DocumentListParams
{
    public string? Status { get; set; }
    public string? TemplateId { get; set; }
    public string? Cursor { get; set; }
    public int? Limit { get; set; }
}

/// <summary>Operations on documents: the core of the API.</summary>
public sealed class DocumentsResource
{
    private static readonly HashSet<string> Terminal = new() { "done", "failed" };

    private readonly PageWeaverHttp _http;

    internal DocumentsResource(PageWeaverHttp http) => _http = http;

    /// <summary>POST /v1/documents. Returns <c>202</c> immediately with the document id and status.</summary>
    public Task<Dictionary<string, JsonElement>> CreateAsync(
        object body, string? idempotencyKey = null, CancellationToken ct = default)
    {
        var headers = idempotencyKey is null
            ? null
            : new Dictionary<string, string> { ["idempotency-key"] = idempotencyKey };
        return _http.RequestJsonAsync(HttpMethod.Post, "/v1/documents", body, headers, ct);
    }

    /// <summary>GET /v1/documents/{id}. When <c>status</c> is "done" it carries a <c>download</c> block.</summary>
    public Task<Dictionary<string, JsonElement>> GetAsync(string id, CancellationToken ct = default)
        => _http.RequestJsonAsync(HttpMethod.Get, $"/v1/documents/{PageWeaverHttp.Enc(id)}", null, null, ct);

    /// <summary>GET /v1/documents/{id}/verify. The SHA-256 content hash + hash-chain position.</summary>
    public Task<Dictionary<string, JsonElement>> VerifyAsync(string id, CancellationToken ct = default)
        => _http.RequestJsonAsync(HttpMethod.Get, $"/v1/documents/{PageWeaverHttp.Enc(id)}/verify", null, null, ct);

    /// <summary>GET /v1/documents. One page of the document history, newest first.</summary>
    public Task<Dictionary<string, JsonElement>> ListAsync(
        DocumentListParams? parameters = null, CancellationToken ct = default)
    {
        parameters ??= new DocumentListParams();
        var query = PageWeaverHttp.BuildQuery(
            ("status", parameters.Status),
            ("templateId", parameters.TemplateId),
            ("cursor", parameters.Cursor),
            ("limit", parameters.Limit));
        return _http.RequestJsonAsync(HttpMethod.Get, "/v1/documents" + query, null, null, ct);
    }

    /// <summary>GET /v1/documents. One page of the document history, newest first.</summary>
    public Task<Dictionary<string, JsonElement>> ListAsync(
        string? status = null,
        string? templateId = null,
        string? cursor = null,
        int? limit = null,
        CancellationToken ct = default)
        => ListAsync(new DocumentListParams
        {
            Status = status,
            TemplateId = templateId,
            Cursor = cursor,
            Limit = limit,
        }, ct);

    /// <summary>Iterate every document across all pages, following <c>nextCursor</c>.</summary>
    public async Task<List<JsonElement>> ListAllAsync(
        DocumentListParams? parameters = null, CancellationToken ct = default)
    {
        parameters ??= new DocumentListParams();
        var all = new List<JsonElement>();
        string? cursor = parameters.Cursor;
        do
        {
            var page = await ListAsync(new DocumentListParams
            {
                Status = parameters.Status,
                TemplateId = parameters.TemplateId,
                Cursor = cursor,
                Limit = parameters.Limit,
            }, ct).ConfigureAwait(false);

            if (page.TryGetValue("items", out var items) && items.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in items.EnumerateArray())
                    all.Add(item);
            }

            cursor = page.TryGetValue("nextCursor", out var nc) && nc.ValueKind == JsonValueKind.String
                ? nc.GetString()
                : null;
        }
        while (!string.IsNullOrEmpty(cursor));

        return all;
    }

    /// <summary>Iterate every document across all pages, following <c>nextCursor</c>.</summary>
    public Task<List<JsonElement>> ListAllAsync(
        string? status = null,
        string? templateId = null,
        int? limit = null,
        CancellationToken ct = default)
        => ListAllAsync(new DocumentListParams
        {
            Status = status,
            TemplateId = templateId,
            Limit = limit,
        }, ct);

    /// <summary>POST /v1/documents/{id}/regenerate. Faithfully replay a prior document.</summary>
    public Task<Dictionary<string, JsonElement>> RegenerateAsync(string id, CancellationToken ct = default)
        => _http.RequestJsonAsync(
            HttpMethod.Post, $"/v1/documents/{PageWeaverHttp.Enc(id)}/regenerate", null, null, ct);

    /// <summary>Poll a document until it reaches a terminal state (done/failed) or the timeout elapses.</summary>
    public async Task<Dictionary<string, JsonElement>> WaitForAsync(
        string id, WaitOptions? opts = null, CancellationToken ct = default)
    {
        opts ??= new WaitOptions();
        var deadline = DateTime.UtcNow + opts.Timeout;
        var delay = opts.IntervalMs;

        var last = await GetAsync(id, ct).ConfigureAwait(false);
        while (!IsTerminal(last, out var status))
        {
            if (DateTime.UtcNow >= deadline)
                throw new PageWeaverTimeoutException(id, status, opts.Timeout.TotalMilliseconds);

            var remaining = deadline - DateTime.UtcNow;
            var wait = delay < remaining ? delay : remaining;
            if (wait > TimeSpan.Zero)
                await Task.Delay(wait, ct).ConfigureAwait(false);

            var nextMs = delay.TotalMilliseconds * opts.Backoff;
            var capMs = opts.MaxIntervalMs.TotalMilliseconds;
            delay = TimeSpan.FromMilliseconds(nextMs < capMs ? nextMs : capMs);

            last = await GetAsync(id, ct).ConfigureAwait(false);
        }

        if (IsFailed(last) && opts.ThrowOnFailure)
            throw new PageWeaverDocumentFailedException(last);

        return last;
    }

    /// <summary>Convenience: <see cref="CreateAsync"/> then <see cref="WaitForAsync"/>.</summary>
    public async Task<Dictionary<string, JsonElement>> CreateAndWaitAsync(
        object body, WaitOptions? opts = null, CancellationToken ct = default)
    {
        var created = await CreateAsync(body, null, ct).ConfigureAwait(false);
        if (!created.TryGetValue("id", out var idEl) || idEl.ValueKind != JsonValueKind.String)
            return created;
        return await WaitForAsync(idEl.GetString()!, opts, ct).ConfigureAwait(false);
    }

    /// <summary>Create a document synchronously: send <c>Prefer: wait</c> so the server holds the
    /// response open until the render finishes. Content-negotiated: PDF bytes, a finished document, or a
    /// 202 fallback whose id you then poll.</summary>
    public async Task<SyncResult> CreateSyncAsync(object body, bool pdf = false, CancellationToken ct = default)
    {
        var headers = new Dictionary<string, string> { ["prefer"] = "wait" };
        var accept = pdf ? "application/pdf" : "application/json";

        using var resp = await _http.RequestRawAsync(HttpMethod.Post, "/v1/documents", body, headers, accept, ct)
            .ConfigureAwait(false);

        var contentType = resp.Content.Headers.ContentType?.MediaType ?? string.Empty;
        if (contentType.IndexOf("application/pdf", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return new SyncResult
            {
                Kind = "pdf",
                Id = FirstHeader(resp, "x-document-id"),
                Version = ParseIntOrNull(FirstHeader(resp, "x-document-version")),
                Pdf = await resp.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false),
            };
        }

        var raw = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        var bodyDict = string.IsNullOrEmpty(raw)
            ? new Dictionary<string, JsonElement>()
            : JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(raw) ?? new Dictionary<string, JsonElement>();

        if ((int)resp.StatusCode == 202)
        {
            return new SyncResult
            {
                Kind = "pending",
                Id = bodyDict.TryGetValue("id", out var idEl) && idEl.ValueKind == JsonValueKind.String
                    ? idEl.GetString()
                    : null,
                Version = bodyDict.TryGetValue("version", out var vEl) && vEl.ValueKind == JsonValueKind.Number
                    ? vEl.GetInt32()
                    : null,
                Status = bodyDict.TryGetValue("status", out var sEl) && sEl.ValueKind == JsonValueKind.String
                    ? sEl.GetString()
                    : null,
            };
        }

        return new SyncResult { Kind = "document", Document = bodyDict };
    }

    /// <summary>GET /v1/documents/{id}/pages. Per-page geometry + text/thumbnail availability.</summary>
    public Task<List<JsonElement>> PagesAsync(string id, CancellationToken ct = default)
        => _http.RequestJsonArrayAsync(
            HttpMethod.Get, $"/v1/documents/{PageWeaverHttp.Enc(id)}/pages", null, null, ct);

    /// <summary>POST /v1/documents/{id}/migrate-comments. Carry open comment threads forward.</summary>
    public Task<Dictionary<string, JsonElement>> MigrateCommentsAsync(
        string id, object body, CancellationToken ct = default)
        => _http.RequestJsonAsync(
            HttpMethod.Post, $"/v1/documents/{PageWeaverHttp.Enc(id)}/migrate-comments", body, null, ct);

    /// <summary>GET /v1/documents/{id}/comment-migration. The comment-migration rollup.</summary>
    public Task<Dictionary<string, JsonElement>> CommentMigrationAsync(string id, CancellationToken ct = default)
        => _http.RequestJsonAsync(
            HttpMethod.Get, $"/v1/documents/{PageWeaverHttp.Enc(id)}/comment-migration", null, null, ct);

    /// <summary>Download the finished PDF bytes. For a download-protected document, pass <paramref name="password"/>
    /// (fetches the password-gated content endpoint, no API key). Otherwise the signed URL is resolved and
    /// fetched automatically.</summary>
    public async Task<byte[]> DownloadAsync(string id, string? password = null, CancellationToken ct = default)
    {
        if (password is not null)
        {
            var headers = new Dictionary<string, string> { ["x-document-password"] = password };
            return await _http.RequestBytesAsync(
                HttpMethod.Get, $"/v1/documents/{PageWeaverHttp.Enc(id)}/content", headers, noAuth: true, ct)
                .ConfigureAwait(false);
        }

        var doc = await GetAsync(id, ct).ConfigureAwait(false);
        var status = doc.TryGetValue("status", out var st) && st.ValueKind == JsonValueKind.String
            ? st.GetString()
            : null;

        if (status != "done"
            || !doc.TryGetValue("download", out var dl)
            || dl.ValueKind != JsonValueKind.Object
            || !dl.TryGetProperty("url", out var urlEl)
            || urlEl.ValueKind != JsonValueKind.String)
        {
            throw new PageWeaverDocumentFailedException(doc);
        }

        if (dl.TryGetProperty("protected", out var prot)
            && prot.ValueKind == JsonValueKind.True)
        {
            throw new PageWeaverDocumentFailedException(doc);
        }

        return await _http.FetchUrlBytesAsync(urlEl.GetString()!, ct).ConfigureAwait(false);
    }

    private static bool IsTerminal(Dictionary<string, JsonElement> doc, out string? status)
    {
        status = doc.TryGetValue("status", out var st) && st.ValueKind == JsonValueKind.String
            ? st.GetString()
            : null;
        return status is not null && Terminal.Contains(status);
    }

    private static bool IsFailed(Dictionary<string, JsonElement> doc)
        => doc.TryGetValue("status", out var st)
           && st.ValueKind == JsonValueKind.String
           && st.GetString() == "failed";

    private static string? FirstHeader(HttpResponseMessage resp, string name)
        => resp.Headers.TryGetValues(name, out var values)
            ? System.Linq.Enumerable.FirstOrDefault(values)
            : null;

    private static int? ParseIntOrNull(string? value)
        => int.TryParse(value, System.Globalization.NumberStyles.Integer,
            System.Globalization.CultureInfo.InvariantCulture, out var n) ? n : null;
}
