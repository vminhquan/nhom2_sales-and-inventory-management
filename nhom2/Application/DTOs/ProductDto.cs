namespace nhom2.Application.DTOs
{
    public class ProductDto
    {
        public int Id { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal SellingPrice { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int ReserveStock { get; set; }
    }

    public class ReserveStockRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public class ReserveStockResponse
    {
        public required ProductDto Product { get; set; }
        public int PreviousQuantity { get; set; }
        public int CurrentQuantity { get; set; }
    }

    public class UpdateInventoryRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public int ReserveStock { get; set; }
    }
}
