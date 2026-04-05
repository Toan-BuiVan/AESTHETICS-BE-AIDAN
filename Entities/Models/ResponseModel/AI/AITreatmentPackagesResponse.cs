namespace Aesthetics.Entities.Models.ResponseModel.AI
{
    public class AITreatmentPackagesResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public ServicePackageInfo Service { get; set; }
        public List<TreatmentPackageInfo> TreatmentPackages { get; set; } = new();
    }

    public class ServicePackageInfo
    {
        public int ServiceId { get; set; }
        public string ServiceName { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int? Duration { get; set; }
        public string ServiceImage { get; set; }
    }

    public class TreatmentPackageInfo
    {
        public int PlanId { get; set; }
        public string PlanName { get; set; }
        public int? TotalSessions { get; set; }
        public decimal? Price { get; set; }
        public int? SessionInterval { get; set; }
        public string Description { get; set; }
        public List<TreatmentSessionInfo> Sessions { get; set; } = new();
    }

    public class TreatmentSessionInfo
    {
        public int SessionId { get; set; }
        public int? SessionNumber { get; set; }
        public string SessionName { get; set; }
        public string Description { get; set; }
        public int? Duration { get; set; }
    }
}