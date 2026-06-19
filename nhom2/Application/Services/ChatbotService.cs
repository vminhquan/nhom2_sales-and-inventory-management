using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using nhom2.Application.DTOs;
using nhom2.Domain.Entities;
using nhom2.Domain.Interfaces;

namespace nhom2.Application.Services;

public class ChatbotService : IChatbotService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IChatSession _chatRepository;
    private readonly IProductClient _productClient;
    private readonly IOrderService _orderService;
    private readonly IUserClient _userClient;
    private readonly HttpClient _http;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ChatbotService> _logger;

    public ChatbotService(
        IChatSession chatRepository,
        IProductClient productClient,
        IOrderService orderService,
        IUserClient userClient,
        HttpClient http,
        IConfiguration configuration,
        ILogger<ChatbotService> logger)
    {
        _chatRepository = chatRepository;
        _productClient = productClient;
        _orderService = orderService;
        _userClient = userClient;
        _http = http;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<ChatSessionDto> GetSessionAsync(int customerUserId, string? customerEmail)
    {
        var session = await _chatRepository.GetOrCreateActiveAsync(customerUserId, customerEmail);
        var messages = await _chatRepository.GetMessagesAsync(session.Id);
        return new ChatSessionDto
        {
            Id = session.Id,
            Messages = messages.Select(MapMessage).ToList()
        };
    }

    public async Task<ChatResponseDto> SendAsync(int customerUserId, string? customerEmail, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Tin nhắn không được để trống.");

        var session = await _chatRepository.GetOrCreateActiveAsync(customerUserId, customerEmail);
        await _chatRepository.AddMessageAsync(new ChatMessage
        {
            ChatSessionId = session.Id,
            Role = "user",
            Content = message.Trim()
        });

        var products = await _productClient.GetProductsAsync();
        var orders = await _orderService.GetOrdersByUserIdAsync(customerUserId);
        if (orders.Count == 0 && !string.IsNullOrWhiteSpace(customerEmail))
            orders = await _orderService.GetOrdersByCustomerEmailAsync(customerEmail);

        var membership = !string.IsNullOrWhiteSpace(customerEmail)
            ? await _userClient.GetCustomerMembershipByEmailAsync(customerEmail)
            : null;
        var context = BuildContext(products, orders, membership);
        var history = await _chatRepository.GetMessagesAsync(session.Id, 12);
        var reply = await AskGeminiAsync(context, history);
        var actions = BuildActions(reply, products);

        await _chatRepository.AddMessageAsync(new ChatMessage
        {
            ChatSessionId = session.Id,
            Role = "assistant",
            Content = reply
        });

        var messages = await _chatRepository.GetMessagesAsync(session.Id);
        return new ChatResponseDto
        {
            SessionId = session.Id,
            Reply = reply,
            Actions = actions,
            Messages = messages.Select(MapMessage).ToList()
        };
    }

    public Task EndSessionAsync(int customerUserId) => _chatRepository.EndActiveAsync(customerUserId);

    private async Task<string> AskGeminiAsync(string dataContext, IReadOnlyCollection<ChatMessage> history)
    {
        var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("GEMINI_API_KEY is not configured.");

        var model = _configuration["Gemini:Model"];
        if (string.IsNullOrWhiteSpace(model))
            model = "gemini-2.5-flash";

        var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{Uri.EscapeDataString(model)}:generateContent";
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Add("x-goog-api-key", apiKey);
        request.Content = JsonContent.Create(new
        {
            systemInstruction = new
            {
                parts = new[]
                {
                    new
                    {
                        text = "Bạn là chatbot hỗ trợ khách hàng của Smart Sale Store. "
                            + "Chỉ trả lời dựa trên DATA_THAT được cung cấp. Không tự bịa giá, tồn kho, "
                            + "khuyến mãi, hạng thành viên hoặc trạng thái đơn hàng. Nếu dữ liệu không có, "
                            + "hãy nói rõ chưa có dữ liệu. Trả lời ngắn gọn bằng tiếng Việt. Khi gợi ý sản phẩm, "
                            + "hãy ghi ID sản phẩm dạng #123 để frontend tạo nút mở hoặc thêm vào giỏ hàng."
                    }
                }
            },
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[] { new { text = dataContext } }
                }
            }.Concat(history.Select(message => new
            {
                role = message.Role == "assistant" ? "model" : "user",
                parts = new[] { new { text = message.Content } }
            })),
            generationConfig = new
            {
                temperature = 0.2,
                maxOutputTokens = 700
            }
        });

        using var response = await _http.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            var message = GetGeminiErrorMessage(content);
            _logger.LogWarning(
                "Gemini API returned {StatusCode}. Message: {Message}",
                (int)response.StatusCode,
                message);
            throw new InvalidOperationException(
                $"Gemini API error: {(int)response.StatusCode} {response.StatusCode}. {message}");
        }

        using var document = JsonDocument.Parse(content);
        var parts = document.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts");
        var reply = string.Join("", parts.EnumerateArray()
            .Where(part => part.TryGetProperty("text", out _))
            .Select(part => part.GetProperty("text").GetString()));
        return string.IsNullOrWhiteSpace(reply)
            ? "Mình chưa tạo được câu trả lời lúc này."
            : reply.Trim();
    }

    private static string BuildContext(
        IReadOnlyCollection<ProductDto> products,
        IReadOnlyCollection<OrderResponseDto> orders,
        CustomerMembershipDto? membership)
    {
        var saleProducts = products
            .Where(product => product.SalePrice.HasValue && product.SalePrice.Value > 0 && product.SalePrice.Value < product.OriginalPrice)
            .Take(20);
        var productLines = products
            .OrderBy(product => product.Name)
            .Take(60)
            .Select(product =>
                $"#{product.Id} | {product.Name} | DM: {product.CategoryName} | Giá gốc: {product.OriginalPrice:0} | Giá bán: {product.SellingPrice:0} | SalePrice: {(product.SalePrice.HasValue ? product.SalePrice.Value.ToString("0") : "không")} | Tồn: {product.Quantity} | Giữ kho: {product.ReserveStock}");
        var saleLines = saleProducts.Select(product => $"#{product.Id} {product.Name} còn {product.Quantity}, giá bán {product.SellingPrice:0}, giá gốc {product.OriginalPrice:0}");
        var orderLines = orders
            .OrderByDescending(order => order.CreatedAt)
            .Take(20)
            .Select(order =>
                $"Đơn #{order.Id} | Trạng thái: {order.Status} | Thanh toán: {order.PaymentMethod} | Tổng: {order.Total:0} | Đã trả: {order.AmountPaid:0} | Công nợ: {order.DebtAmount:0} | Ngày: {order.CreatedAt:dd/MM/yyyy} | SP: {string.Join(", ", order.OrderItems.Select(item => $"{item.ProductName} x{item.Quantity}"))}");

        var tierLine = membership is null
            ? "Chưa có dữ liệu hạng thành viên."
            : $"Hạng: {membership.TierLabel}, số đơn đã thanh toán: {membership.PaidOrderCount}, giảm giá thành viên: {membership.DiscountPercent:0}% khi đơn có tổng số lượng từ 3 sản phẩm.";

        return string.Join("\n", new[]
        {
            "DATA_THAT:",
            "Hạng khách hàng:",
            tierLine,
            "Sản phẩm:",
            string.Join("\n", productLines),
            "Sản phẩm đang khuyến mãi:",
            string.Join("\n", saleLines.DefaultIfEmpty("Không có sản phẩm khuyến mãi trong dữ liệu hiện tại.")),
            "Đơn hàng của chính khách hàng:",
            string.Join("\n", orderLines.DefaultIfEmpty("Khách hàng chưa có đơn hàng trong dữ liệu hiện tại."))
        });
    }

    private static List<ChatActionDto> BuildActions(string reply, IReadOnlyCollection<ProductDto> products)
    {
        var ids = products
            .Where(product => reply.Contains($"#{product.Id}", StringComparison.OrdinalIgnoreCase))
            .Select(product => product.Id)
            .Distinct()
            .Take(4)
            .ToList();

        var result = new List<ChatActionDto>();
        foreach (var id in ids)
        {
            var product = products.First(item => item.Id == id);
            result.Add(new ChatActionDto
            {
                Type = "open-product",
                ProductId = id,
                Label = $"Xem {product.Name}"
            });
            if (product.Quantity > 0)
            {
                result.Add(new ChatActionDto
                {
                    Type = "add-to-cart",
                    ProductId = id,
                    Label = $"Thêm {product.Name} vào giỏ"
                });
            }
        }

        return result;
    }

    private static ChatMessageDto MapMessage(ChatMessage message) => new()
    {
        Role = message.Role,
        Content = message.Content,
        CreatedAt = message.CreatedAt
    };

    private static string GetGeminiErrorMessage(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return "Gemini không trả về nội dung lỗi.";

        try
        {
            using var document = JsonDocument.Parse(content);
            if (document.RootElement.TryGetProperty("error", out var error))
            {
                if (error.TryGetProperty("message", out var message))
                    return message.GetString() ?? "Gemini trả về lỗi không có message.";
            }
        }
        catch
        {
            // Use the short raw body below.
        }

        return content.Length > 300 ? content[..300] : content;
    }
}
