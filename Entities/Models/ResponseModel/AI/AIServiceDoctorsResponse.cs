using System.Collections.Generic;

namespace Aesthetics.Entities.Models.ResponseModel.AI
{
    public class AIServiceDoctorsResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string ServiceName { get; set; }
        public string ServiceDescription { get; set; }
        public List<AIServiceDoctor> Doctors { get; set; }
    }

    public class AIServiceDoctor
    {
        public int StaffId { get; set; }
        public string Name { get; set; }
        public string Specialization { get; set; }
        public int Experience { get; set; }
        public string Degree { get; set; }
        public decimal Rating { get; set; }
        public int AppointmentCount { get; set; }
    }
}