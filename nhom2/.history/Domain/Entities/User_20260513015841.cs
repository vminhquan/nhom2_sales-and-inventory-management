using System;
using System.Collections.Generic;

namespace nhom2.Domain.Entities
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<Order> Orders { get; set; } = new();
    }
}
