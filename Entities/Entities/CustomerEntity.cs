using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Entities
{
	[Table("Customers")]
	public class CustomerEntity : Aesthetics.Entities.BaseEntity.BaseEntity
	{
		/// <summary>FK → Accounts: liên kết tài khoản đăng nhập</summary>
		public int? AccountId { get; set; }

		/// <summary>Họ tên khách hàng</summary>
		[MaxLength(250)]
		public string? FullName { get; set; }

		/// <summary>Ngày sinh</summary>
		public DateTime? DateBirth { get; set; }

		/// <summary>Giới tính: 'Nam', 'Nu', 'Khac'</summary>
		[MaxLength(20)]
		public string? Sex { get; set; }

		/// <summary>Số điện thoại (duy nhất)</summary>
		[MaxLength(250)]
		public string? Phone { get; set; }

		/// <summary>Địa chỉ</summary>
		[MaxLength(250)]
		public string? Address { get; set; }

		/// <summary>Email (duy nhất)</summary>
		[MaxLength(250)]
		public string? Email { get; set; }

		/// <summary>Số CCCD/CMND (duy nhất)</summary>
		[MaxLength(250)]
		public string? IDCard { get; set; }

		/// <summary>Mã giới thiệu của khách này (dùng để chia sẻ)</summary>
		[MaxLength(15)]
		public string? ReferralCode { get; set; }

		/// <summary>Điểm tích lũy từ giới thiệu người khác</summary>
		public int AccumulatedPoints { get; set; }

		/// <summary>Điểm thứ hạng (tích từ mua hàng/dịch vụ)</summary>
		public int RatingPoints { get; set; }

		/// <summary>Thứ hạng: ThanhVien, Bac, Vang, KimCuong</summary>
		[MaxLength(200)]
		public string? RankMember { get; set; } 

		// Navigation properties
		[ForeignKey(nameof(AccountId))]
		public virtual AccountEntity? Account { get; set; }

		public virtual ICollection<AppointmentEntity> Appointments { get; set; } = [];
		public virtual ICollection<CartEntity> Carts { get; set; } = [];
		public virtual ICollection<WalletEntity> Wallets { get; set; } = [];
		public virtual ICollection<CommentEntity> Comments { get; set; } = [];
		public virtual ICollection<InvoiceEntity> Invoices { get; set; } = [];
		public virtual ICollection<CustomerTreatmentPlanEntity> CustomerTreatmentPlans { get; set; } = [];
		public virtual ICollection<CustomerPaymentInfoEntity>? PaymentInfos { get; set; } = [];
		public virtual ICollection<AddressInfoEntity> Addresses { get; set; } = new List<AddressInfoEntity>();
	}
}
