# PageWeaver .NET SDK

Official .NET client for the [PageWeaver](https://pageweaver.io) PDF generation API. No third-party dependencies (uses `HttpClient` + `System.Text.Json`). Targets .NET 8.

## Install

```bash
dotnet add package PageWeaver
```

## Usage

```csharp
using PageWeaver;

var pw = new PageWeaverClient("pk_live_...");

// Create a document and wait for it to finish rendering
var doc = await pw.CreateAndWaitAsync(new
{
    templateId = "tmpl_invoice",
    payload = new { number = "INV-001", total = 4200 },
});
Console.WriteLine(doc["status"].GetString()); // "done"

// Or fire-and-poll yourself
var created = await pw.CreateDocumentAsync(new { html = "<h1>Hello</h1>" });
var result = await pw.GetDocumentAsync(created["id"].GetString()!);
```

Non-2xx responses throw `PageWeaverException` (with `.StatusCode` and `.Body`).

## License

MIT
