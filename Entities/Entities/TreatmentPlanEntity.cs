using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Entities
{
	[Table("TreatmentPlans")]
	public class TreatmentPlanEntity : Aesthetics.Entities.BaseEntity.BaseEntity
	{
		/// <summary>FK → Services: thuộc dịch vụ nào</summary>
		public int? ServiceId { get; set; }

		/// <summary>Tên gói: 'Gói 5 buổi Trị Nám', 'Gói 10 buổi Premium'</summary>
		[Required]
		[MaxLength(250)]
		public string? PlanName { get; set; } = string.Empty;

		/// <summary>Tổng số buổi trong gói: 5, 10, 15...</summary>
		public int? TotalSessions { get; set; }

		/// <summary>Giá trọn gói (thường rẻ hơn mua lẻ)</summary>
		[Column(TypeName = "decimal(18,2)")]
		public decimal? Price { get; set; }

		/// <summary>Khoảng cách khuyến nghị giữa các buổi (số ngày): 7, 14, 30</summary>
		public int? SessionInterval { get; set; }

		/// <summary>Mô tả gói: 'Giảm 20% so với mua lẻ'</summary>
		public string? Description { get; set; }

		// Navigation properties
		[ForeignKey(nameof(ServiceId))]
		public virtual ServiceEntity? Service { get; set; }

		public virtual ICollection<TreatmentSessionEntity> TreatmentSessions { get; set; } = [];
		public virtual ICollection<CustomerTreatmentPlanEntity> CustomerTreatmentPlans { get; set; } = [];
		//public virtual ICollection<CartProductEntity> CartProducts { get; set; } = [];
		public virtual ICollection<InvoiceDetailEntity> InvoiceDetails { get; set; } = [];
		public virtual ICollection<InvoiceEntity> Invoices { get; set; } = [];
	}
}
