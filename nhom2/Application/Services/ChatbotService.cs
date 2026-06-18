using System.Net.Http.Headers;
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

    public ChatbotService(
        IChatSession chatRepository,
        IProductClient productClient,
        IOrderService orderService,
        IUserClient userClient,
        HttpClient http,
        IConfiguration configuration)
    {
        _chatRepository = chatRepository;
        _productClient = productClient;
        _orderService = orderService;
        _userClient = userClient;
        _http = http;
        _configuration = configuration;
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
        var reply = await AskOpenAiAsync(context, history);
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

    private async Task<string> AskOpenAiAsync(string dataContext, IReadOnlyCollection<ChatMessage> history)
    {
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("OPENAI_API_KEY is not configured.");

        var model = _configuration["OpenAI:Model"];
        if (string.IsNullOrWhiteSpace(model))
            model = "gpt-4o-mini";

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = JsonContent.Create(new
        {
            model,
            temperature = 0.2,
            messages = new List<object>
            {
                new
                {
                    role = "system",
                    content = "Bạn là chatbot hỗ trợ khách hàng của Smart Sale Store. Chỉ trả lời dựa trên DATA_THAT bên dưới. Không tự bịa giá, tồn kho, khuyến mãi, hạng thành viên hoặc trạng thái đơn. Nếu dữ liệu không có, nói rõ là chưa có dữ liệu. Trả lời ngắn gọn bằng tiếng Việt. Khi gợi ý sản phẩm, hãy ghi ID sản phẩm dạng #123 để frontend tạo nút mở/thêm giỏ."
                },
                new { role = "system", content = dataContext }
            }.Concat(history.Select(message => new
            {
                role = message.Role == "assistant" ? "assistant" : "user",
                content = message.Content
            }))
        });

        using var response = await _http.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"OpenAI API error: {response.StatusCode}");

        using var document = JsonDocument.Parse(content);
        return document.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString()
            ?.Trim() ?? "Mình chưa tạo được câu trả lời lúc này.";
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
}
