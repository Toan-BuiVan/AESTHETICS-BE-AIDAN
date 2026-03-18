using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Entities
{
	[Table("Suppliers")]
	public class SupplierEntity : Aesthetics.Entities.BaseEntity.BaseEntity
	{
		/// <summary>Tên nhà cung cấp</summary>
		[MaxLength(200)]
		public string? SupplierName { get; set; }

		/// <summary>Địa chỉ</summary>
		[MaxLength(250)]
		public string? Address { get; set; }

		/// <summary>Số điện thoại</summary>
		[MaxLength(250)]
		public string? Phone { get; set; }

		/// <summary>Email liên hệ</summary>
		[MaxLength(250)]
		public string? Email { get; set; }

		// Navigation properties
		public virtual ICollection<ProductEntity> Products { get; set; } = [];
	}
}
