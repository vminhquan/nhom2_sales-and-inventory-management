using System;
using System.Collections.Generic;
using nhom2.Domain.Entities;

namespace nhom2.Application.DTOs
{
    public class ProductDto
    {
        public int Id { get; set; }
        public string TenSanPham { get; set; } = string.Empty;
        public string MaSanPham { get; set; } = string.Empty;
        public decimal Gia { get; set; }
        public int SoLuongTon { get; set; }
        public string DanhMuc { get; set; } = string.Empty;
        public string MoTa { get; set; } = string.Empty;
    }
    public class ReserveStockRequest
    {
        
    }
    public class ReserveStockResponse
    {

    }
}