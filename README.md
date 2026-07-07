# PageWeaver .NET SDK

Official .NET client for the [PageWeaver](https://pageweaver.io) PDF generation API. No third-party dependencies (uses `HttpClient` + `System.Text.Json`). Targets .NET 8.

## Install

```bash
dotnet add package PageWeaver
```

## Getting started

```csharp
using PageWeaver;

var pw = new PageWeaverClient("pk_live_...");
// var pw = new PageWeaverClient("pk_test_...", "http://localhost:4000"); // dev
```

The client exposes resource groups, each returning `Dictionary<string, JsonElement>` for object
responses and `List<JsonElement>` for arrays:

`pw.Documents` · `pw.Templates` (+ `pw.Templates.Proposals`) · `pw.Schemas` · `pw.Usage` ·
`pw.Comments` · `pw.Reviews` · `pw.ShareLinks` · `pw.Environments` · `pw.Deployments`.

Non-2xx responses throw `PageWeaverException` (with `.StatusCode` and `.Body`). Waiting on a document
may throw `PageWeaverTimeoutException` or `PageWeaverDocumentFailedException`.

## Documents

```csharp
// Create and wait for the render to finish, then download the PDF bytes.
var doc = await pw.Documents.CreateAndWaitAsync(new
{
    templateId = "tmpl_invoice",
    payload = new { number = "INV-001", total = 4200 },
});
Console.WriteLine(doc["status"].GetString()); // "done"
byte[] pdf = await pw.Documents.DownloadAsync(doc["id"].GetString()!);

// Fire-and-poll yourself.
var created = await pw.Documents.CreateAsync(new { html = "<h1>Hello</h1>" });
var result = await pw.Documents.GetAsync(created["id"].GetString()!);

// Synchronous create (server holds the response open). Ask for raw bytes with pdf: true.
var sync = await pw.Documents.CreateSyncAsync(new { templateId = "tmpl_invoice", payload = new { total = 42 } }, pdf: true);
if (sync.Kind == "pdf") await File.WriteAllBytesAsync("invoice.pdf", sync.Pdf!);
else if (sync.Kind == "pending") await pw.Documents.WaitForAsync(sync.Id!);

// Verify, regenerate, list, and page through history.
var proof = await pw.Documents.VerifyAsync(doc["id"].GetString()!);
var page = await pw.Documents.ListAsync(status: "done", limit: 50);
var all = await pw.Documents.ListAllAsync(status: "failed"); // follows nextCursor

// Download a protected document with its password (uses the content endpoint, no API key).
byte[] protectedPdf = await pw.Documents.DownloadAsync(doc["id"].GetString()!, password: "s3cret");
```

`WaitForAsync` / `CreateAndWaitAsync` accept a `WaitOptions` (interval, backoff, timeout,
`ThrowOnFailure`).

## Templates and proposals

```csharp
var templates = await pw.Templates.ListAsync();
var versions = await pw.Templates.VersionsAsync("tmpl_invoice");
var source = await pw.Templates.VersionAsync("tmpl_invoice", 3, include: "source");

var proposal = await pw.Templates.Proposals.OpenAsync("tmpl_invoice", new { fromDraft = true });
await pw.Templates.Proposals.ApproveAsync("tmpl_invoice", proposal["id"].GetString()!);
await pw.Templates.Proposals.PromoteAsync("tmpl_invoice", proposal["id"].GetString()!);
```

## Schemas, usage

```csharp
var schemas = await pw.Schemas.ListAsync();
var schema = await pw.Schemas.GetAsync("sch_invoice", version: 2);
var usage = await pw.Usage.GetAsync();
```

## Comments, reviews, share links

```csharp
var thread = await pw.Comments.CreateAsync(new { documentId = "doc_1", body = "Fix the total", anchor = new { /* ... */ } });
var threads = await pw.Comments.ListAsync("doc_1", status: "open");
await pw.Comments.ReplyAsync(thread["id"].GetString()!, new { body = "Done" });
await pw.Comments.ResolveAsync(thread["id"].GetString()!);

var review = await pw.Reviews.CreateAsync(new { documentId = "doc_1" });
await pw.Reviews.ApproveAsync(review["id"].GetString()!, new { });

var link = await pw.ShareLinks.CreateAsync(new { documentId = "doc_1", capabilities = new[] { "view" } });
await pw.ShareLinks.DisableAsync(link["id"].GetString()!);
```

## Environments and deployments

```csharp
await pw.Environments.SetPinAsync("production", "tmpl_invoice", 3);
var pins = await pw.Environments.PinsAsync("production");
await pw.Environments.PromoteAsync("production", new { from = "staging" });

var plan = await pw.Deployments.PlanAsync(new { manifest = "...", files = new { } });
var applied = await pw.Deployments.ApplyAsync(plan["id"].GetString()!);
```

## Verifying webhooks

Each delivery is signed with `HMACSHA256(secret, rawBody)` in the `x-pageweaver-signature` header,
formatted `sha256=<hex>`. Verify the **raw** request body (not a re-serialized object):

```csharp
if (!Webhooks.VerifySignature(secret, rawBody, signatureHeader))
    return Results.Unauthorized();

// Or parse-on-verify (throws PageWeaverWebhookSignatureException on mismatch):
var evt = Webhooks.VerifyWebhook(secret, rawBody, signatureHeader);
Console.WriteLine(evt["event"].GetString()); // "document.completed"
```

Header names are available as `Webhooks.SignatureHeader`, `Webhooks.EventHeader`, and
`Webhooks.TimestampHeader`.

## License

MIT
