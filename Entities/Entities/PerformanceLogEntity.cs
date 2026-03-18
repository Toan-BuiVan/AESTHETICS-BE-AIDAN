using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Entities
{
	[Table("PerformanceLogs")]
	public class PerformanceLogEntity : Aesthetics.Entities.BaseEntity.BaseEntity
	{
		/// <summary>FK → Staffs: nhân viên nào</summary>
		public int? StaffId { get; set; }

		/// <summary>FK → Invoices: liên kết hóa đơn tạo ra doanh thu</summary>
		public int? InvoiceId { get; set; }

		/// <summary>Hoa hồng (VNĐ)</summary>
		[Column(TypeName = "decimal(18,2)")]
		public decimal Commission { get; set; }

		/// <summary>Thưởng (VNĐ)</summary>
		[Column(TypeName = "decimal(18,2)")]
		public decimal Bonus { get; set; }

		/// <summary>Ngày ghi nhận</summary>
		public DateTime? LogDate { get; set; }

		/// <summary>Mô tả: 'Hoa hồng từ dịch vụ Trị Nám'</summary>
		public string? Description { get; set; }

		// Navigation properties
		[ForeignKey(nameof(StaffId))]
		public virtual StaffEntity? Staff { get; set; }

		[ForeignKey(nameof(InvoiceId))]
		public virtual InvoiceEntity? Invoice { get; set; }
	}
}
