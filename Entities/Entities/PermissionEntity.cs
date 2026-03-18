using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Entities
{
	[Table("Permissions")]
	public class PermissionEntity : Aesthetics.Entities.BaseEntity.BaseEntity
	{
		/// <summary>FK → Accounts: tài khoản được phân quyền</summary>
		public int? AccountId { get; set; }

		/// <summary>FK → Functions: chức năng được gán</summary>
		public int? FunctionId { get; set; }

		/// <summary>true = Đang kích hoạt, false = Tạm tắt</summary>
		public bool IsActive { get; set; } = true;

		// Navigation properties
		[ForeignKey(nameof(AccountId))]
		public virtual AccountEntity? Account { get; set; }

		[ForeignKey(nameof(FunctionId))]
		public virtual FunctionEntity? Function { get; set; }
	}
}
