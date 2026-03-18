using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Entities
{
	[Table("Functions")]
	public class FunctionEntity : Aesthetics.Entities.BaseEntity.BaseEntity
	{
		/// <summary>Mã chức năng: 'FUNC_BOOKING', 'FUNC_INVOICE'...</summary>
		[MaxLength(250)]
		public string? FunctionCode { get; set; }

		/// <summary>Tên hiển thị: 'Quản lý lịch hẹn'</summary>
		[MaxLength(250)]
		public string? FunctionName { get; set; }

		/// <summary>Mô tả chức năng</summary>
		public string? Description { get; set; }

		// Navigation properties
		public virtual ICollection<PermissionEntity> Permissions { get; set; } = [];
	}
}
