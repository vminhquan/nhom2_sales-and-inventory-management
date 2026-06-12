using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using nhom2.Application.DTOs;

namespace nhom2.Application.Services;

public class ProductClient : IProductClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;
    private readonly string _internalApiKey;

    public ProductClient(HttpClient http, IConfiguration configuration)
    {
        _http = http;
        _internalApiKey = configuration["Services:InternalApiKey"]
            ?? throw new InvalidOperationException("Services:InternalApiKey is not configured.");
    }

    public async Task<List<ProductDto>> GetProductsAsync()
    {
        using var response = await _http.GetAsync("/api/products");
        response.EnsureSuccessStatusCode();
        return await ReadAsync<List<ProductDto>>(response) ?? new List<ProductDto>();
    }

    public async Task<ProductDto?> GetProductByIdAsync(int id)
    {
        using var response = await _http.GetAsync($"/api/products/{id}");
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await ReadAsync<ProductDto>(response);
    }

    public async Task<ReserveStockResponse> ReserveStockAsync(ReserveStockRequest request)
    {
        if (request.Quantity <= 0)
            throw new ArgumentException("Số lượng giữ kho phải lớn hơn 0");

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/inventory/reserve")
        {
            Content = JsonContent.Create(request)
        };
        httpRequest.Headers.Add("X-Internal-Api-Key", _internalApiKey);
        using var response = await _http.SendAsync(httpRequest);
        response.EnsureSuccessStatusCode();

        return await ReadAsync<ReserveStockResponse>(response)
            ?? throw new InvalidOperationException("Product service trả về dữ liệu giữ kho không hợp lệ");
    }

    public async Task ReleaseStockAsync(int productId, int quantity)
    {
        if (quantity <= 0)
            return;

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/inventory/release")
        {
            Content = JsonContent.Create(new ReserveStockRequest
            {
                ProductId = productId,
                Quantity = quantity
            })
        };
        request.Headers.Add("X-Internal-Api-Key", _internalApiKey);
        using var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    private static async Task<T?> ReadAsync<T>(HttpResponseMessage response)
        where T : class
    {
        var content = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(content);

        if (document.RootElement.ValueKind == JsonValueKind.Object
            && document.RootElement.TryGetProperty("data", out var data))
        {
            return data.Deserialize<T>(JsonOptions);
        }

        return document.RootElement.Deserialize<T>(JsonOptions);
    }
}
