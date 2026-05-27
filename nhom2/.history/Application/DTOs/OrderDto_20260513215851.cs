using System;
using System.Collections.Generic;

namespace nhom2.Application.DTOs
{
    /// <summary>
    /// DTO để tạo đơn hàng mới từ client
    /// </summary>
    public class CreateOrderDto
    {
        public int UserId { get; set; }
        public List<OrderItemDto> OrderItems { get; set; } = new();
    }

    /// <summary>
    /// DTO để cập nhật đơn hàng
    /// </summary>
    public class UpdateOrderDto
    {
        public int Id { get; set; }
        public string? Status { get; set; }
    }

    /// <summary>
    /// DTO cho chi tiết đơn hàng
    /// </summary>
    public class OrderItemDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }

    /// <summary>
    /// DTO để trả về thông tin đơn hàng cho client
    /// </summary>
    public class OrderResponseDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Status { get; set; }
        public decimal Total { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastModifiedAt { get; set; }
        public List<OrderItemResponseDto> OrderItems { get; set; } = new();
    }

    /// <summary>
    /// DTO để trả về chi tiết item trong đơn hàng
    /// </summary>
    public class OrderItemResponseDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal SubTotal { get; set; }
    }
}
