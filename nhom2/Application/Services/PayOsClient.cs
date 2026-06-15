using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace nhom2.Application.Services;

public class PayOsClient
{
    private readonly HttpClient _http;
    private readonly IConfiguration _configuration;

    public PayOsClient(HttpClient http, IConfiguration configuration)
    {
        _http = http;
        _configuration = configuration;
    }

    public void EnsureConfigured()
    {
        _ = GetRequired("PayOS:ClientId");
        _ = GetRequired("PayOS:ApiKey");
        _ = GetRequired("PayOS:ChecksumKey");
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
        var clientId = GetRequired("PayOS:ClientId");
        var apiKey = GetRequired("PayOS:ApiKey");
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
        request.Headers.Add("x-client-id", clientId);
        request.Headers.Add("x-api-key", apiKey);
        using var response = await _http.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(content);
        var root = document.RootElement;
        if (root.GetProperty("code").GetString() != "00")
            throw new PayOsException(
                root.GetProperty("desc").GetString() ?? "PayOS rejected payment");

        return root.GetProperty("data").GetProperty("checkoutUrl").GetString()
            ?? throw new PayOsException("PayOS did not return checkoutUrl");
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
        var checksumKey = GetRequired("PayOS:ChecksumKey");
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(checksumKey));
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

    private string GetRequired(string key)
    {
        var value = _configuration[key];
        if (string.IsNullOrWhiteSpace(value) || value.StartsWith("CHANGE_ME", StringComparison.Ordinal))
            throw new PayOsConfigurationException(
                $"{key} is not configured. Configure the corresponding PayOS__ environment variable.");
        return value;
    }
}

public class PayOsConfigurationException : InvalidOperationException
{
    public PayOsConfigurationException(string message) : base(message)
    {
    }
}

public class PayOsException : InvalidOperationException
{
    public PayOsException(string message) : base(message)
    {
    }
}
