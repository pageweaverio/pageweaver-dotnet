using System;
using System.Security.Cryptography;
using System.Text;
using PageWeaver;
using Xunit;

public class ClientTests
{
    [Fact]
    public void RequiresApiKey()
    {
        Assert.Throws<ArgumentException>(() => new PageWeaverClient(""));
    }

    [Fact]
    public void ConstructsWithApiKey()
    {
        var client = new PageWeaverClient("pk_test_x");
        Assert.NotNull(client);
    }

    [Fact]
    public void ExposesAllResourcesNonNull()
    {
        var pw = new PageWeaverClient("pk_test_x");
        Assert.NotNull(pw.Documents);
        Assert.NotNull(pw.Templates);
        Assert.NotNull(pw.Templates.Proposals);
        Assert.NotNull(pw.Schemas);
        Assert.NotNull(pw.Usage);
        Assert.NotNull(pw.Comments);
        Assert.NotNull(pw.Reviews);
        Assert.NotNull(pw.ShareLinks);
        Assert.NotNull(pw.Environments);
        Assert.NotNull(pw.Deployments);
    }

    [Fact]
    public void ExceptionCarriesStatusAndBody()
    {
        var e = new PageWeaverException("boom", 402, "quota");
        Assert.Equal(402, e.StatusCode);
        Assert.Equal("quota", e.Body);
    }

    [Fact]
    public void TimeoutExceptionIsPageWeaverException()
    {
        var e = new PageWeaverTimeoutException("doc_1", "queued", 5000);
        Assert.IsAssignableFrom<PageWeaverException>(e);
        Assert.Equal("doc_1", e.DocumentId);
        Assert.Equal("queued", e.LastStatus);
    }

    [Fact]
    public void WebhookSignatureRoundTrips()
    {
        const string secret = "whsec_test";
        const string body = "{\"event\":\"document.completed\",\"documentId\":\"doc_1\"}";

        var signature = Webhooks.Sign(secret, body);
        Assert.StartsWith("sha256=", signature);

        // A freshly-signed body verifies true.
        Assert.True(Webhooks.VerifySignature(secret, body, signature));

        // The signature matches an independently computed HMAC-SHA256 hex digest.
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
        var expected = "sha256=" + Convert.ToHexString(hash).ToLowerInvariant();
        Assert.Equal(expected, signature);

        // Wrong secret fails.
        Assert.False(Webhooks.VerifySignature("whsec_wrong", body, signature));

        // Missing signature fails, never throws.
        Assert.False(Webhooks.VerifySignature(secret, body, null));
    }

    [Fact]
    public void VerifyWebhookThrowsOnMismatch()
    {
        const string secret = "whsec_test";
        const string body = "{\"event\":\"document.failed\"}";

        Assert.Throws<PageWeaverWebhookSignatureException>(
            () => Webhooks.VerifyWebhook(secret, body, "sha256=deadbeef"));

        var good = Webhooks.Sign(secret, body);
        var parsed = Webhooks.VerifyWebhook(secret, body, good);
        Assert.Equal("document.failed", parsed["event"].GetString());
    }
}
