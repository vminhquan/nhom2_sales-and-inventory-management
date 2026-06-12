using nhom2.Application.DTOs;

namespace nhom2.Application.Services;

public interface IOrderEventPublisher
{
    Task PublishAsync(OrderEventDto orderEvent);
}
