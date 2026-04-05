namespace Aesthetics.Entities.Models.ResponseModel.AI
{
    public class AIBestDoctorResponse
    {
		public bool Success { get; set; }
		public string Message { get; set; }
		public string TreatmentPlanName { get; set; }

		// Thông tin bác sĩ
		public int StaffId { get; set; }
		public string StaffName { get; set; }
		public string Specialization { get; set; }
		public string Degree { get; set; }
		public int? ExperienceYears { get; set; }
		public string StaffImage { get; set; }

		// Số lịch đặt
		public int AppointmentCount { get; set; }
	}
}