using System.Net.Http;

namespace PageWeaver;

/// <summary>Client for the PageWeaver document-generation API. Endpoints are grouped into resource
/// properties (<see cref="Documents"/>, <see cref="Templates"/>, etc.).</summary>
///
/// <example>
/// <code>
/// var pw = new PageWeaverClient("pk_live_...");
/// var doc = await pw.Documents.CreateAndWaitAsync(new { templateId = "tmpl_invoice", payload = new { total = 42 } });
/// var pdf = await pw.Documents.DownloadAsync(doc["id"].GetString()!);
/// </code>
/// </example>
public sealed class PageWeaverClient
{
    private const string DefaultBaseUrl = "https://api.pageweaver.io";

    /// <summary>Operations on documents: the core of the API.</summary>
    public DocumentsResource Documents { get; }

    /// <summary>Read-only discovery of templates and their versions (plus <c>Templates.Proposals</c>).</summary>
    public TemplatesResource Templates { get; }

    /// <summary>Read-only discovery of the JSON Schemas your payloads validate against.</summary>
    public SchemasResource Schemas { get; }

    /// <summary>Current-period page usage against your plan quota.</summary>
    public UsageResource Usage { get; }

    /// <summary>Anchored comment threads on documents (requires a <c>review</c>-scoped key for writes).</summary>
    public CommentsResource Comments { get; }

    /// <summary>Review requests + approvals on documents (requires a <c>review</c>-scoped key for writes).</summary>
    public ReviewsResource Reviews { get; }

    /// <summary>Capability-scoped external share links (requires a <c>review</c>-scoped key).</summary>
    public ShareLinksResource ShareLinks { get; }

    /// <summary>Named per-account environments + pins over template versions (requires a <c>deploy</c>-scoped key for writes).</summary>
    public EnvironmentsResource Environments { get; }

    /// <summary>Plan and apply documents-as-code deployments from a manifest (requires a <c>deploy</c>-scoped key for writes).</summary>
    public DeploymentsResource Deployments { get; }

    /// <summary>Create a client.</summary>
    /// <param name="apiKey">Your secret API key (<c>pk_live_...</c> / <c>pk_test_...</c>).</param>
    /// <param name="baseUrl">API base URL. Defaults to <c>https://api.pageweaver.io</c>.</param>
    /// <param name="httpClient">Optional <see cref="HttpClient"/> to reuse; one is created otherwise.</param>
    public PageWeaverClient(string apiKey, string baseUrl = DefaultBaseUrl, HttpClient? httpClient = null)
    {
        var http = new PageWeaverHttp(apiKey, baseUrl, httpClient);
        Documents = new DocumentsResource(http);
        Templates = new TemplatesResource(http);
        Schemas = new SchemasResource(http);
        Usage = new UsageResource(http);
        Comments = new CommentsResource(http);
        Reviews = new ReviewsResource(http);
        ShareLinks = new ShareLinksResource(http);
        Environments = new EnvironmentsResource(http);
        Deployments = new DeploymentsResource(http);
    }
}
