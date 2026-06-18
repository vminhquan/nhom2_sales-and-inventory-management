namespace nhom2.Application.DTOs;

public class ChatMessageDto
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class ChatSessionDto
{
    public int Id { get; set; }
    public List<ChatMessageDto> Messages { get; set; } = new();
}

public class ChatRequestDto
{
    public string Message { get; set; } = string.Empty;
}

public class ChatActionDto
{
    public string Type { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public string Label { get; set; } = string.Empty;
}

public class ChatResponseDto
{
    public int SessionId { get; set; }
    public string Reply { get; set; } = string.Empty;
    public List<ChatActionDto> Actions { get; set; } = new();
    public List<ChatMessageDto> Messages { get; set; } = new();
}
