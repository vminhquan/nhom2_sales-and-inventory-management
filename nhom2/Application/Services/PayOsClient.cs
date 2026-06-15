using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace nhom2.Application.Services;

public class PayOsClient
{
    private readonly HttpClient _http;
    private readonly string _clientId;
    private readonly string _apiKey;
    private readonly string _checksumKey;

    public PayOsClient(HttpClient http, IConfiguration configuration)
    {
        _http = http;
        _clientId = GetRequired(configuration, "PayOS:ClientId");
        _apiKey = GetRequired(configuration, "PayOS:ApiKey");
        _checksumKey = GetRequired(configuration, "PayOS:ChecksumKey");
    }

    public async Task<string> CreatePaymentLinkAsync(
        long orderCode,
        int amount,
        string description,
        string returnUrl,
        string cancelUrl,
        long expiredAt,
        IReadOnlyCollection<object> items)
    {
        var signatureData = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["amount"] = amount.ToString(CultureInfo.InvariantCulture),
            ["cancelUrl"] = cancelUrl,
            ["description"] = description,
            ["orderCode"] = orderCode.ToString(CultureInfo.InvariantCulture),
            ["returnUrl"] = returnUrl
        };
        var payload = new
        {
            orderCode,
            amount,
            description,
            items,
            cancelUrl,
            returnUrl,
            expiredAt,
            signature = Sign(string.Join("&", signatureData.Select(value => $"{value.Key}={value.Value}")))
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "/v2/payment-requests")
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Add("x-client-id", _clientId);
        request.Headers.Add("x-api-key", _apiKey);
        using var response = await _http.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(content);
        var root = document.RootElement;
        if (root.GetProperty("code").GetString() != "00")
            throw new InvalidOperationException(root.GetProperty("desc").GetString() ?? "PayOS rejected payment");

        return root.GetProperty("data").GetProperty("checkoutUrl").GetString()
            ?? throw new InvalidOperationException("PayOS did not return checkoutUrl");
    }

    public bool VerifyWebhook(JsonElement payload)
    {
        if (!payload.TryGetProperty("data", out var data)
            || !payload.TryGetProperty("signature", out var signatureElement))
            return false;

        var values = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var property in data.EnumerateObject())
            values[property.Name] = ToSignatureValue(property.Value);

        var expected = Sign(string.Join("&", values.Select(value => $"{value.Key}={value.Value}")));
        var actual = signatureElement.GetString() ?? string.Empty;
        return expected.Length == actual.Length
            && CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expected.ToLowerInvariant()),
                Encoding.UTF8.GetBytes(actual.ToLowerInvariant()));
    }

    private string Sign(string value)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_checksumKey));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    }

    private static string ToSignatureValue(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Null => string.Empty,
        JsonValueKind.String => value.GetString() ?? string.Empty,
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        _ => value.GetRawText()
    };

    private static string GetRequired(IConfiguration configuration, string key)
    {
        var value = configuration[key];
        if (string.IsNullOrWhiteSpace(value) || value.StartsWith("CHANGE_ME", StringComparison.Ordinal))
            throw new InvalidOperationException($"{key} is not configured.");
        return value;
    }
}
