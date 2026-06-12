using System.ComponentModel.DataAnnotations.Schema;

namespace nhom2.Domain.Entities
{
    public class Order
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int? CustomerId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastModifiedAt { get; set; }
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public decimal DiscountAmount { get; set; }
        public decimal AmountPaid { get; set; }
        public Customer? Customer { get; set; }
        public List<OrderItem> OrderItems { get; set; } = new();

        [NotMapped]
        public decimal Subtotal => OrderItems.Sum(item => item.SubTotal);

        [NotMapped]
        public decimal TotalAmount => Math.Max(0, Subtotal - DiscountAmount);

        [NotMapped]
        public decimal DebtAmount => Math.Max(0, TotalAmount - AmountPaid);
    }
}
