namespace nhom2.Domain.Entities
{
    public enum OrderStatus
    {
        Pending,      // Chờ xử lý
        Processing,   // Đang xử lý
        Completed,    // Hoàn tất
        Cancelled     // Đã hủy
    }
}