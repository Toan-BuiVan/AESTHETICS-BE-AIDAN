using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Models.RequestModel
{
	/// <summary>DTO để định nghĩa sản phẩm cho từng buổi</summary>
	public class SessionProductDefinition
	{
		/// <summary>Buổi thứ mấy (1, 2, 3...)</summary>
		public int SessionNumber { get; set; }

		/// <summary>Danh sách sản phẩm cho buổi này</summary>
		public List<SessionProductItem> Products { get; set; } = new();
	}

	/// <summary>Thông tin sản phẩm cho một buổi</summary>
	public class SessionProductItem
	{
		/// <summary>ID sản phẩm</summary>
		public int ProductId { get; set; }

		/// <summary>Số lượng sử dụng</summary>
		public int QuantityUsed { get; set; } = 1;
	}

	public class CreateTreatmentPlan
	{
		public int? ServiceId { get; set; }
		public string? PlanName { get; set; }
		public int? TotalSessions { get; set; }
		public decimal? Price { get; set; }
		public int? SessionInterval { get; set; }
		public string? Description { get; set; }

		/// <summary>Định nghĩa sản phẩm cho từng buổi. Nếu null thì không tạo SessionProducts</summary>
		public List<SessionProductDefinition>? SessionProducts { get; set; }
	}

	public class UpdateTreatmentPlan
	{
		public int? Id { get; set; }
		public int? ServiceId { get; set; }
		public string? PlanName { get; set; }
		public int? TotalSessions { get; set; }
		public decimal? Price { get; set; }
		public int? SessionInterval { get; set; }
		public string? Description { get; set; }
	}

	public class DeleteTreatmentPlan
	{
		public int? Id { get; set; }
	}

	public class TreatmentPlanGet : BaseSearchModel
	{
		public int? Id { get; set; }
		public int? ServiceId { get; set; }
	}
}
