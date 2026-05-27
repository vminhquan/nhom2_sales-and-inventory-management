namespace nhom2.Application.Mock
{
    /// <summary>
    /// Mock dữ liệu Product - dùng để test khi Product Service chưa sẵn sàng
    /// </summary>
    public class MockProductData
    {
        // Dữ liệu giả lập Product
        private static readonly List<ProductMockDto> Products = new()
        {
            new ProductMockDto { Id = 1, Name = "Laptop Dell XPS 13", Price = 1200000, Stock = 10 },
            new ProductMockDto { Id = 2, Name = "iPhone 15 Pro", Price = 25000000, Stock = 5 },
            new ProductMockDto { Id = 3, Name = "Samsung Galaxy S24", Price = 22000000, Stock = 8 },
            new ProductMockDto { Id = 4, Name = "iPad Air", Price = 15000000, Stock = 12 },
            new ProductMockDto { Id = 5, Name = "AirPods Pro", Price = 6000000, Stock = 20 },
            new ProductMockDto { Id = 6, Name = "Apple Watch Series 9", Price = 8000000, Stock = 15 },
            new ProductMockDto { Id = 7, Name = "Sony WH-1000XM5", Price = 5000000, Stock = 25 },
            new ProductMockDto { Id = 8, Name = "Gaming Mouse Razer", Price = 2000000, Stock = 30 },
        };

        /// <summary>
        /// Lấy product theo ID
        /// </summary>
        public static ProductMockDto? GetProductById(int productId)
        {
            return Products.FirstOrDefault(p => p.Id == productId);
        }

        /// <summary>
        /// Lấy tất cả products
        /// </summary>
        public static List<ProductMockDto> GetAllProducts()
        {
            return Products;
        }

        /// <summary>
        /// Kiểm tra sản phẩm có còn stock không
        /// </summary>
        public static bool HasStock(int productId, int quantity)
        {
            var product = GetProductById(productId);
            return product != null && product.Stock >= quantity;
        }
    }

    /// <summary>
    /// DTO để represent Product trong mock data
    /// </summary>
    public class ProductMockDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Stock { get; set; }
    }
}
