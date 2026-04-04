using System.Collections.Generic;

namespace Aesthetics.Entities.Models.ResponseModel.AI
{
    public class AIProductsByPriceResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public List<AIProductPrice> Products { get; set; }
    }

    public class AIProductPrice
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal Price { get; set; }
        public string Description { get; set; }
        public int Quantity { get; set; }
        public int ServiceTypeId { get; set; }
    }
}