using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Entities
{
	[Table("ServiceTypes")]
	public class ServiceTypeEntity : Aesthetics.Entities.BaseEntity.BaseEntity
	{
		/// <summary>Tên loại: 'Massage', 'ChamSocDa', 'TriNam'...</summary>
		[MaxLength(250)]
		public string? ServiceTypeName { get; set; }

		/// <summary>Phân loại: 'DichVu' hoặc 'SanPham'</summary>
		[MaxLength(20)]
		public int? ServiceCategory { get; set; }

		/// <summary>Mô tả chi tiết về loại dịch vụ</summary>
		public string? Description { get; set; }

		// Navigation properties
		public virtual ICollection<ServiceEntity> Services { get; set; } = [];
		public virtual ICollection<ProductEntity> Products { get; set; } = [];
	}
}
