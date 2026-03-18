using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Entities
{
	[Table("AccountSessions")]
	public class AccountSessionEntity : Aesthetics.Entities.BaseEntity.BaseEntity
	{
		/// <summary>FK → Accounts: phiên của tài khoản nào</summary>
		public int? AccountId { get; set; }

		/// <summary>Access Token hoặc Session Token</summary>
		public string? Token { get; set; }

		/// <summary>Tên thiết bị: 'iPhone 15', 'Chrome Windows'</summary>
		[MaxLength(250)]
		public string? DeviceName { get; set; }

		/// <summary>Địa chỉ IP đăng nhập</summary>
		[MaxLength(250)]
		public string? IP { get; set; }

		/// <summary>Thời điểm tạo phiên</summary>
		public DateTime? CreateTime { get; set; }

		/// <summary>Lần truy cập cuối cùng</summary>
		public DateTime? LastAccess { get; set; }

		// Navigation properties
		[ForeignKey(nameof(AccountId))]
		public virtual AccountEntity? Account { get; set; }
	}
}
