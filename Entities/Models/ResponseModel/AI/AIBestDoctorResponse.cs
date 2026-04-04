namespace Aesthetics.Entities.Models.ResponseModel.AI
{
    public class AIBestDoctorResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string TreatmentPlanName { get; set; }
        public int StaffId { get; set; }
        public int AppointmentCount { get; set; }
    }
}