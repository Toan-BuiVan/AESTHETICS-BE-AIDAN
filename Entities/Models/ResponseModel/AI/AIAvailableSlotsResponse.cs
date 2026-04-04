using System;
using System.Collections.Generic;

namespace Aesthetics.Entities.Models.ResponseModel.AI
{
    public class AIAvailableSlotsResponse
    {
		public bool Success { get; set; }
		public string Message { get; set; }
		public string DoctorName { get; set; }
		public int? DoctorId { get; set; }
		public List<AIAvailableSlot> AvailableSlots { get; set; }
		public List<AITreatmentPlanInfo> TreatmentPlans { get; set; }
	}

	public class AITreatmentPlanInfo
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public decimal? Price { get; set; }
	}

	public class AIAvailableSlot
    {
		public string Date { get; set; }
		public string StartTime { get; set; }  
		public string EndTime { get; set; }    
		public DateTime SlotDateTime { get; set; }
	}
}