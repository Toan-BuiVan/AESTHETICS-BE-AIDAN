namespace Aesthetics.Entities.Models.ResponseModel.AI
{
    public class AIBookAppointmentResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int AppointmentId { get; set; }
    }
}