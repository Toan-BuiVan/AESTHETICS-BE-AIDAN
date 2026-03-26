using Aesthetics.Entities.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Models.ResponseModel
{
	public class TreatmentPlanResponseModel
	{
		public TreatmentPlanInfomation? TreatmentPlanInfomation { get; set; }
		public ServiceInfomation? ServiceInformation { get; set; }
		public List<TreatmentSessionInformation>? TreatmentSessionInformation { get; set; }
		public List<SessionProductInformation>? SessionProductInformation { get; set; }
	}

	public class TreatmentPlanInfomation 
	{
		public int Id { get; set; }
		public bool DeleteStatus { get; set; }

		public int? ServiceId { get; set; }

		/// <summary>Tên gói: 'Gói 5 buổi Trị Nám', 'Gói 10 buổi Premium'</summary>
		public string? PlanName { get; set; } = string.Empty;

		/// <summary>Tổng số buổi trong gói: 5, 10, 15...</summary>
		public int? TotalSessions { get; set; }

		/// <summary>Giá trọn gói (thường rẻ hơn mua lẻ)</summary>
		public decimal? Price { get; set; }

		/// <summary>Khoảng cách khuyến nghị giữa các buổi (số ngày): 7, 14, 30</summary>
		public int? SessionInterval { get; set; }

		/// <summary>Mô tả gói: 'Giảm 20% so với mua lẻ'</summary>
		public string? Description { get; set; }
	}

	public class ServiceInfomation 
	{
		public string? ServiceName { get; set; }
		public int? ServiceId { get; set; }
		public int? ServiceTypeId { get; set; }
		public string? ServiceImage { get; set; }
		public decimal? Price { get; set; }
		public int? Duration { get; set; }
		public bool? IsCourse { get; set; }
	}

	public class TreatmentSessionInformation 
	{
		public int? TreatmentSessionId { get; set; }
		public int? SessionNumber { get; set; }

		/// <summary>Tên buổi: 'Buổi 1: Tẩy da chết', 'Buổi 2: Laser nhẹ'</summary>
		public string? SessionName { get; set; }

		/// <summary>Mô tả chi tiết quy trình buổi này</summary>
		public string? Description { get; set; }

		/// <summary>Thời lượng buổi (phút) — có thể khác nhau mỗi buổi</summary>
		public int? Duration { get; set; }
	}

	public class SessionProductInformation
	{
		public int? SessionProductId { get; set; }
		public int? ProductId { get; set; }
		public string? ProductName { get; set; }
		public int? QuantityUsed { get; set; }
		public int? ServiceId { get; set; }
	}
}
