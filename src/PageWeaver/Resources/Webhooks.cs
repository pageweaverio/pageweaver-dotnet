using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PageWeaver;

/// <summary>Verify PageWeaver webhook deliveries. Each delivery is signed with HMAC-SHA256 over the exact
/// request body, keyed by your account webhook secret (<c>whsec_...</c>), and sent in the
/// <c>x-pageweaver-signature</c> header formatted <c>sha256=&lt;hex&gt;</c>. This mirrors the server's
/// signer and has no dependencies.</summary>
public static class Webhooks
{
    /// <summary>Header carrying the <c>sha256=&lt;hex&gt;</c> signature.</summary>
    public const string SignatureHeader = "x-pageweaver-signature";

    /// <summary>Header carrying the event name.</summary>
    public const string EventHeader = "x-pageweaver-event";

    /// <summary>Header carrying the unix-seconds send time.</summary>
    public const string TimestampHeader = "x-pageweaver-timestamp";

    /// <summary>Compute the <c>sha256=&lt;hex&gt;</c> signature for a body. Exposed mainly for tests.</summary>
    public static string Sign(string secret, string body)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
        var sb = new StringBuilder("sha256=", 7 + hash.Length * 2);
        foreach (var b in hash)
            sb.Append(b.ToString("x2", System.Globalization.CultureInfo.InvariantCulture));
        return sb.ToString();
    }

    /// <summary>Constant-time check of a <c>sha256=&lt;hex&gt;</c> signature against the raw body. Never throws.</summary>
    public static bool VerifySignature(string secret, string body, string? signature)
    {
        if (string.IsNullOrEmpty(signature)) return false;
        var expected = Encoding.UTF8.GetBytes(Sign(secret, body));
        var actual = Encoding.UTF8.GetBytes(signature);
        return expected.Length == actual.Length && CryptographicOperations.FixedTimeEquals(expected, actual);
    }

    /// <summary>Verify a webhook signature and return the parsed event body. Throws
    /// <see cref="PageWeaverWebhookSignatureException"/> when the signature is missing or wrong.</summary>
    public static Dictionary<string, JsonElement> VerifyWebhook(string secret, string body, string? signature)
    {
        if (!VerifySignature(secret, body, signature))
            throw new PageWeaverWebhookSignatureException();
        return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(body)
               ?? new Dictionary<string, JsonElement>();
    }
}
