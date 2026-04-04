namespace Aesthetics.Entities.Models.ResponseModel.AI
{
    public class AICancelAppointmentResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int CancelledCount { get; set; }
    }
}