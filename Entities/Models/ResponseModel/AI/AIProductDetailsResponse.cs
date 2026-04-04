namespace Aesthetics.Entities.Models.ResponseModel.AI
{
    public class AIProductDetailsResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public int UserCount { get; set; }
    }
}