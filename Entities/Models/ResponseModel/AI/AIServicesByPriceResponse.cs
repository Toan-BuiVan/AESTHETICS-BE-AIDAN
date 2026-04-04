using System.Collections.Generic;

namespace Aesthetics.Entities.Models.ResponseModel.AI
{
    public class AIServicesByPriceResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public List<AIServicePrice> Services { get; set; }
    }

    public class AIServicePrice
    {
        public int ServiceId { get; set; }
        public string ServiceName { get; set; }
        public decimal Price { get; set; }
        public string Description { get; set; }
        public int ServiceTypeId { get; set; }
    }
}