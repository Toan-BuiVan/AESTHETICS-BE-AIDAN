namespace Aesthetics.Entities.Models.ResponseModel
{
    public class CreateServiceResponseModel
    {
        public bool Success { get; set; }
        public int? ServiceId { get; set; }
        public string? Message { get; set; }
    }
}