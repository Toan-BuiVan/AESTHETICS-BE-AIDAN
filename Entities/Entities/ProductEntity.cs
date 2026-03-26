using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Entities
{
	[Table("Products")]
	public class ProductEntity : Aesthetics.Entities.BaseEntity.BaseEntity
	{
		/// <summary>FK → ServiceTypes: thuộc loại sản phẩm nào</summary>
		public int? ServiceTypeId { get; set; }

		/// <summary>FK → Suppliers: nhà cung cấp</summary>
		public int? SupplierId { get; set; }

		/// <summary>Tên sản phẩm: 'Kem chống nắng SPF50'</summary>
		[MaxLength(250)]
		public string? ProductName { get; set; }

		/// <summary>Mô tả sản phẩm</summary>
		public string? Description { get; set; }

		/// <summary>Giá bán cho khách</summary>
		[Column(TypeName = "decimal(18,2)")]
		public decimal? SellingPrice { get; set; }

		/// <summary>Số lượng tồn kho hiện tại</summary>
		public int Quantity { get; set; }

		/// <summary>Đơn vị: 'chai', 'hộp', 'tuýp', 'miếng'</summary>
		[MaxLength(50)]
		public string? Unit { get; set; }

		/// <summary>Ngưỡng tồn kho tối thiểu → cảnh báo hết hàng</summary>
		public int MinimumStock { get; set; }

		/// <summary>URL hình ảnh sản phẩm</summary>
		public string? ProductImages { get; set; }

		/// <summary>Giá vốn (giá nhập) → tính lợi nhuận</summary>
		[Column(TypeName = "decimal(18,2)")]
		public decimal? CostPrice { get; set; }

		// Navigation properties
		[ForeignKey(nameof(ServiceTypeId))]
		public virtual ServiceTypeEntity? ServiceType { get; set; }

		[ForeignKey(nameof(SupplierId))]
		public virtual SupplierEntity? Supplier { get; set; }

		public virtual ICollection<SessionProductEntity> SessionProducts { get; set; } = [];
		public virtual ICollection<CommentEntity> Comments { get; set; } = [];
		public virtual ICollection<CartProductEntity> CartProductEntitys { get; set; } = [];
		public virtual ICollection<InvoiceEntity> Invoices { get; set; } = [];
		public virtual ICollection<InvoiceDetailEntity> InvoiceDetails { get; set; } = [];
	}
}
