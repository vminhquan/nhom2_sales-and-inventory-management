using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace nhom2.Domain.Entities
{
    public class OrderItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }

        public Order? Order { get; set; }

        public OrderItem(int quantity, decimal price)
        {
        if (quantity <= 0) throw new ArgumentException("Số lượng phải lớn hơn 0");
        if (price < 0)throw new ArgumentException("Giá không được là số âm");
        
        Quantity = quantity;
        Price = price;
        }
    public OrderItem() { }
    public decimal SubTotal => Quantity * Price;
    }
   
}