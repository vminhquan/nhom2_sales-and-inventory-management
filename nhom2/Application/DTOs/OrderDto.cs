using System;
using System.Collections.Generic;
using nhom2.Domain.Entities;
namespace nhom2.Application.DTOs
{
    /// <summary>
    /// DTO để tạo đơn hàng mới từ client
    /// </summary>
    public class CreateOrderDto
    {
        public int UserId { get; set; }
        public int? CustomerId { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal AmountPaid { get; set; }
        public List<OrderItemDto> OrderItems { get; set; } = new();
    }

    /// <summary>
    /// DTO để cập nhật đơn hàng
    /// </summary>
    public class UpdateOrderDto
    {
        public int Id { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal AmountPaid { get; set; }
        public List<OrderItemDto> OrderItems { get; set; } = new();
    }

    /// <summary>
    /// DTO cho chi tiết đơn hàng
    /// </summary>
    public class OrderItemDto
    {
        public int ProductId { get; set; }
        public int? ProductVariantId { get; set; }
        public int? ProductVariantColorId { get; set; }
        public int Quantity { get; set; }
    }

    /// <summary>
    /// DTO để trả về thông tin đơn hàng cho client
    /// </summary>
    public class OrderResponseDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public string? Status { get; set; }
        public string? PaymentMethod { get; set; }
        public decimal Subtotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal Total { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal DebtAmount { get; set; }
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
        public int? ProductVariantId { get; set; }
        public int? ProductVariantColorId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? VariantName { get; set; }
        public string? ColorName { get; set; }
        public string? Sku { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal SubTotal { get; set; }
    }

    public class UpdateOrderStatusDto
    {
        public int Id { get; set; }
        public OrderStatus Status { get; set; }
    }
}
