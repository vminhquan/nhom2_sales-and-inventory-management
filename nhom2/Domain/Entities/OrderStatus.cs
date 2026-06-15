namespace nhom2.Domain.Entities
{
    public enum OrderStatus
    {
        Pending,      // Chờ xử lý
        PendingPayment,
        ProcessingPayment,
        Paid,
        PaymentCancelled,
        PaymentExpired,
        PaymentFailed,
        Processing,   // Đang xử lý
        Shipped,      // Đang giao
        Completed,    // Hoàn tất
        Cancelled     // Đã hủy
    }
}
