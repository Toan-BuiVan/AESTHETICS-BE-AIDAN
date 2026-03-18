using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Entities
{
	[Table("Wallets")]
	public class WalletEntity : Aesthetics.Entities.BaseEntity.BaseEntity
	{
		/// <summary>FK → Customers: ví của khách nào</summary>
		public int? CustomerId { get; set; }

		/// <summary>FK → Vouchers: voucher nào trong ví</summary>
		public int? VoucherId { get; set; }

		/// <summary>Ngày nhận voucher vào ví</summary>
		public DateTime? ClaimedDate { get; set; }

		/// <summary>false = Chưa dùng, true = Đã dùng</summary>
		public bool IsUsed { get; set; }

		// Navigation properties
		[ForeignKey(nameof(CustomerId))]
		public virtual CustomerEntity? Customer { get; set; }

		[ForeignKey(nameof(VoucherId))]
		public virtual VoucherEntity? Voucher { get; set; }
	}
}
