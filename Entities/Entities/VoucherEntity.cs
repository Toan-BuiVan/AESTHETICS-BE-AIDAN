using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Entities
{
	[Table("Vouchers")]
	public class VoucherEntity : Aesthetics.Entities.BaseEntity.BaseEntity
	{
		/// <summary>Mã voucher duy nhất: 'SALE20', 'VIP50'</summary>
		[MaxLength(50)]
		public string? Code { get; set; }

		/// <summary>Mô tả voucher</summary>
		public string? Description { get; set; }

		/// <summary>Phần trăm giảm giá: VD 20.00 = giảm 20%</summary>
		[Column(TypeName = "decimal(5,2)")]
		public decimal? DiscountValue { get; set; }

		/// <summary>Ngày bắt đầu có hiệu lực</summary>
		public DateTime? StartDate { get; set; }

		/// <summary>Ngày hết hạn</summary>
		public DateTime? EndDate { get; set; }

		/// <summary>Giá trị đơn tối thiểu để áp dụng</summary>
		[Column(TypeName = "decimal(18,2)")]
		public decimal? MinimumOrderValue { get; set; }

		/// <summary>Số tiền giảm tối đa</summary>
		[Column(TypeName = "decimal(18,2)")]
		public decimal? MaxValue { get; set; }

		/// <summary>Thứ hạng yêu cầu: 'Vang', 'KimCuong' (null = tất cả)</summary>
		[MaxLength(200)]
		public string? RankMember { get; set; }

		/// <summary>Số điểm mua hàng cần để đổi voucher</summary>
		public int RatingPoints { get; set; }

		/// <summary>Số điểm giới thiệu cần để đổi voucher</summary>
		public int AccumulatedPoints { get; set; }

		/// <summary>Số lần sử dụng tối đa (toàn hệ thống)</summary>
		public int UsageLimit { get; set; } = 1;

		/// <summary>true = Đang kích hoạt, false = Tạm tắt</summary>
		public bool IsActive { get; set; } = true;

		// Navigation properties
		public virtual ICollection<WalletEntity> Wallets { get; set; } = [];
		public virtual ICollection<InvoiceEntity> Invoices { get; set; } = [];
		public virtual ICollection<InvoiceDetailEntity> InvoiceDetails { get; set; } = [];
	}
}
