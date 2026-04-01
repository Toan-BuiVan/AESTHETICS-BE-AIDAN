namespace Aesthetics.Commons.DTOs
{
    public class CartProductDetailDTO
    {
        public int Id { get; set; }
        public int? CartId { get; set; }
        public int? ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal? PriceAtAdd { get; set; }
        public DateTime? CreateDate { get; set; }

        // Thông tin sản phẩm
        public string? ProductName { get; set; }
        public string? ProductImages { get; set; }
        public string? Description { get; set; }
        public decimal? SellingPrice { get; set; }
        public string? Unit { get; set; }
    }
}