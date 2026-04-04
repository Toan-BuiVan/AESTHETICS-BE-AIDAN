namespace Aesthetics.Entities.Models.ResponseModel.AI
{
    public class AICartActionResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int CartProductId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
}