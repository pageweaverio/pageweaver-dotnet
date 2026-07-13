using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PageWeaver;

/// <summary>Template proposals: the PR analog for template changes. A proposal freezes a candidate
/// change; it is reviewed and approved, then promoted into a published version. Reached as
/// <c>pw.Templates.Proposals</c>, scoped to a template id passed on each call. Writes require a
/// <c>deploy</c>-scoped key.</summary>
public sealed class ProposalsResource
{
    private readonly PageWeaverHttp _http;

    internal ProposalsResource(PageWeaverHttp http) => _http = http;

    /// <summary>POST /v1/templates/{templateId}/proposals. Open a proposal on a template.</summary>
    public Task<Dictionary<string, JsonElement>> OpenAsync(
        string templateId, object body, CancellationToken ct = default)
        => _http.RequestJsonAsync(
            HttpMethod.Post, $"/v1/templates/{PageWeaverHttp.Enc(templateId)}/proposals", body, null, ct);

    /// <summary>GET /v1/templates/{templateId}/proposals. List a template's proposals, newest first.</summary>
    public Task<Dictionary<string, JsonElement>> ListAsync(
        string templateId,
        string? status = null,
        string? cursor = null,
        int? limit = null,
        CancellationToken ct = default)
    {
        var query = PageWeaverHttp.BuildQuery(("status", status), ("cursor", cursor), ("limit", limit));
        return _http.RequestJsonAsync(
            HttpMethod.Get, $"/v1/templates/{PageWeaverHttp.Enc(templateId)}/proposals" + query, null, null, ct);
    }

    /// <summary>GET /v1/templates/{templateId}/proposals/{proposalId}. Fetch one proposal.</summary>
    public Task<Dictionary<string, JsonElement>> GetAsync(
        string templateId, string proposalId, CancellationToken ct = default)
        => _http.RequestJsonAsync(
            HttpMethod.Get,
            $"/v1/templates/{PageWeaverHttp.Enc(templateId)}/proposals/{PageWeaverHttp.Enc(proposalId)}",
            null, null, ct);

    /// <summary>POST /v1/templates/{templateId}/proposals/{proposalId}/checks. Re-run the render-diff regression.</summary>
    public Task<Dictionary<string, JsonElement>> RerunChecksAsync(
        string templateId, string proposalId, CancellationToken ct = default)
        => RerunChecksAsync(templateId, proposalId, null, ct);

    /// <summary>POST /v1/templates/{templateId}/proposals/{proposalId}/checks. Re-run with an optional policy body.</summary>
    public Task<Dictionary<string, JsonElement>> RerunChecksAsync(
        string templateId, string proposalId, object? body, CancellationToken ct = default)
        => _http.RequestJsonAsync(
            HttpMethod.Post,
            $"/v1/templates/{PageWeaverHttp.Enc(templateId)}/proposals/{PageWeaverHttp.Enc(proposalId)}/checks",
            body, null, ct);

    /// <summary>POST /v1/templates/{templateId}/proposals/{proposalId}/approve. Append an approval decision.</summary>
    public Task<Dictionary<string, JsonElement>> ApproveAsync(
        string templateId, string proposalId, object? body = null, CancellationToken ct = default)
        => _http.RequestJsonAsync(
            HttpMethod.Post,
            $"/v1/templates/{PageWeaverHttp.Enc(templateId)}/proposals/{PageWeaverHttp.Enc(proposalId)}/approve",
            body, null, ct);

    /// <summary>POST /v1/templates/{templateId}/proposals/{proposalId}/reject. Append a rejection decision.</summary>
    public Task<Dictionary<string, JsonElement>> RejectAsync(
        string templateId, string proposalId, object? body = null, CancellationToken ct = default)
        => _http.RequestJsonAsync(
            HttpMethod.Post,
            $"/v1/templates/{PageWeaverHttp.Enc(templateId)}/proposals/{PageWeaverHttp.Enc(proposalId)}/reject",
            body, null, ct);

    /// <summary>POST /v1/templates/{templateId}/proposals/{proposalId}/promote. Promote the candidate.</summary>
    public Task<Dictionary<string, JsonElement>> PromoteAsync(
        string templateId, string proposalId, CancellationToken ct = default)
        => _http.RequestJsonAsync(
            HttpMethod.Post,
            $"/v1/templates/{PageWeaverHttp.Enc(templateId)}/proposals/{PageWeaverHttp.Enc(proposalId)}/promote",
            null, null, ct);

    /// <summary>DELETE /v1/templates/{templateId}/proposals/{proposalId}. Withdraw an open proposal.</summary>
    public Task<Dictionary<string, JsonElement>> RetractAsync(
        string templateId, string proposalId, CancellationToken ct = default)
        => _http.RequestJsonAsync(
            HttpMethod.Delete,
            $"/v1/templates/{PageWeaverHttp.Enc(templateId)}/proposals/{PageWeaverHttp.Enc(proposalId)}",
            null, null, ct);
}
