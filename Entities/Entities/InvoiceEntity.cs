using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Entities
{
	[Table("Invoices")]
	public class InvoiceEntity : Aesthetics.Entities.BaseEntity.BaseEntity
	{
		/// <summary>FK → Customers: khách hàng</summary>
		public int? CustomerId { get; set; }

		/// <summary>FK → Staffs: nhân viên tạo hóa đơn</summary>
		public int? StaffId { get; set; }

		/// <summary>FK → Vouchers: voucher áp dụng (nếu có)</summary>
		public int? VoucherId { get; set; }

		// <summary>FK → TreatmentPlans: gói liệu trình</summary>
		public int? TreatmentPlanId { get; set; }

		/// <summary>FK → Services: dịch vụ</summary>
		public int? ServiceId { get; set; }

		/// <summary>FK → Product: sản phẩm</summary>
		public int? ProductId  { get; set; }

		/// <summary>Phần trăm giảm giá thực tế</summary>
		[Column(TypeName = "decimal(5,2)")]
		public decimal DiscountValue { get; set; }

		/// <summary>Số tiền đã trả</summary>
		public decimal PaidAmount { get; set; }

		/// <summary>Tổng tiền sau giảm giá</summary>
		[Column(TypeName = "decimal(18,2)")]
		public decimal? TotalMoney { get; set; }

		/// <summary>Ngày tạo hóa đơn</summary>
		public DateTime? DateCreated { get; set; }

		/// <summary>ChuaThanhToan, DaThanhToan, HoanTien</summary>
		[MaxLength(50)]
		public string? Status { get; set; } 

		/// <summary>Loại: 'NhapHang' (mua từ supplier), 'BanHang' (bán cho khách)</summary>
		[MaxLength(50)]
		public string? Type { get; set; }

		/// <summary>DangXuLy, DaGiao, DaHuy</summary>
		[MaxLength(50)]
		public string? OrderStatus { get; set; } 

		/// <summary>TienMat, ChuyenKhoan, TheNganHang, MoMo, VNPay</summary>
		[MaxLength(50)]
		public string? PaymentMethod { get; set; }

		/// <summary>Số tiền còn nợ (nếu trả góp/nợ)</summary>
		[Column(TypeName = "decimal(18,2)")]
		public decimal OutstandingBalance { get; set; }

		// Navigation properties
		[ForeignKey(nameof(CustomerId))]
		public virtual CustomerEntity? Customer { get; set; }

		[ForeignKey(nameof(StaffId))]
		public virtual StaffEntity? Staff { get; set; }

		[ForeignKey(nameof(VoucherId))]
		public virtual VoucherEntity? Voucher { get; set; }

		public virtual ICollection<InvoiceDetailEntity> InvoiceDetails { get; set; } = [];
		public virtual ICollection<PerformanceLogEntity> PerformanceLogs { get; set; } = [];
		//public virtual ICollection<CustomerTreatmentPlanEntity> CustomerTreatmentPlans { get; set; } = [];
	}
}
