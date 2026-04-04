using System.Collections.Generic;

namespace Aesthetics.Entities.Models.ResponseModel.AI
{
    public class AITopProductsResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public List<AITopProduct> Products { get; set; }
    }

    public class AITopProduct
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal Price { get; set; }
        public int SalesCount { get; set; }
    }
}