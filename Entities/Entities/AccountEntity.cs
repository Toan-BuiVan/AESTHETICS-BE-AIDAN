using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Entities
{
	[Table("Accounts")]
	public class AccountEntity : Aesthetics.Entities.BaseEntity.BaseEntity
	{
		/// <summary>Tên đăng nhập (duy nhất)</summary>
		[MaxLength(250)]
		public string? UserName { get; set; }

		/// <summary>Mật khẩu (phải hash trước khi lưu, VD: BCrypt)</summary>
		[MaxLength(250)]
		public string? PassWord { get; set; }

		/// <summary>Ngày tạo tài khoản</summary>
		public DateTime? Creation { get; set; }

		/// <summary>Vai trò: 0 = Nhân viên, 1 = Admin, 2 = Bác sĩ</summary>
		public int? Role { get; set; }

		/// <summary>Token làm mới cho JWT auth</summary>
		[MaxLength(250)]
		public string? RefreshToken { get; set; }

		/// <summary>Thời điểm hết hạn của RefreshToken</summary>
		public DateTime? TokenExpired { get; set; }

		// Navigation properties
		public virtual CustomerEntity? Customer { get; set; }
		public virtual StaffEntity? Staff { get; set; }
		public virtual ICollection<PermissionEntity> Permissions { get; set; } = [];
		public virtual ICollection<AccountSessionEntity> AccountSessions { get; set; } = [];
	}
}
