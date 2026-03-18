using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Entities
{
	[Table("Services")]
	public class ServiceEntity : Aesthetics.Entities.BaseEntity.BaseEntity
	{
		/// <summary>FK → ServiceTypes: thuộc loại dịch vụ nào</summary>
		public int? ServiceTypeId { get; set; }

		/// <summary>Tên dịch vụ: 'Trị Nám', 'Massage Mặt'</summary>
		[MaxLength(200)]
		public string? ServiceName { get; set; }

		/// <summary>Mô tả chi tiết dịch vụ</summary>
		public string? Description { get; set; }

		/// <summary>URL hình ảnh dịch vụ</summary>
		public string? ServiceImage { get; set; }

		/// <summary>Giá MỖI BUỔI nếu mua lẻ (không theo liệu trình)</summary>
		[Column(TypeName = "decimal(18,2)")]
		public decimal? Price { get; set; }

		/// <summary>Thời lượng 1 buổi (phút)</summary>
		public int? Duration { get; set; }

		/// <summary>false = Dịch vụ đơn lẻ, true = Có liệu trình</summary>
		public int? IsCourse { get; set; }

		// Navigation properties
		[ForeignKey(nameof(ServiceTypeId))]
		public virtual ServiceTypeEntity? ServiceType { get; set; }

		public virtual ICollection<TreatmentPlanEntity> TreatmentPlans { get; set; } = [];
		public virtual ICollection<AppointmentEntity> Appointments { get; set; } = [];
		public virtual ICollection<CommentEntity> Comments { get; set; } = [];
		public virtual ICollection<CartProductEntity> CartProductEntitys { get; set; } = [];
		public virtual ICollection<InvoiceDetailEntity> InvoiceDetails { get; set; } = [];
		public virtual ICollection<AppointmentAssignmentEntity> AppointmentAssignments { get; set; } = [];
	}
}
