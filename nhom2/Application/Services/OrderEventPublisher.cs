using System.Net.Http.Json;
using nhom2.Application.DTOs;

namespace nhom2.Application.Services;

public class OrderEventPublisher : IOrderEventPublisher
{
    private readonly HttpClient _http;
    private readonly string _internalApiKey;

    public OrderEventPublisher(HttpClient http, IConfiguration configuration)
    {
        _http = http;
        _internalApiKey = configuration["Services:InternalApiKey"]
            ?? throw new InvalidOperationException("Services:InternalApiKey is not configured.");
    }

    public async Task PublishAsync(OrderEventDto orderEvent)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/internal/order-events")
        {
            Content = JsonContent.Create(orderEvent)
        };
        request.Headers.Add("X-Internal-Api-Key", _internalApiKey);

        using var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }
}
