namespace Aesthetics.Entities.Models.ResponseModel.AI
{
    public class AIMostUsedServiceResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int ServiceId { get; set; }
        public string ServiceName { get; set; }
        public decimal Price { get; set; }
        public int UserCount { get; set; }
    }
}