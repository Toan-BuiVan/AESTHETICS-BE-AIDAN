using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Entities
{
	[Table("InventoryAlerts")]
	public class InventoryAlertEntity : Aesthetics.Entities.BaseEntity.BaseEntity
	{
		/// <summary>FK → Products: sản phẩm bị thiếu tồn kho</summary>
		public int ProductId { get; set; }

		/// <summary>Số lượng tồn kho hiện tại khi tạo alert</summary>
		public int CurrentQuantity { get; set; }

		/// <summary>Ngưỡng tồn kho tối thiểu</summary>
		public int MinimumStock { get; set; }

		/// <summary>Đã gửi email hay chưa</summary>
		public bool IsEmailSent { get; set; } = false;

		/// <summary>Ngày tạo cảnh báo</summary>
		public DateTime CreatedDate { get; set; }

		/// <summary>Ngày gửi email (nếu đã gửi)</summary>
		public DateTime? EmailSentDate { get; set; }

		/// <summary>Số lần gửi email</summary>
		public int EmailSentCount { get; set; } = 0;

		// Navigation properties
		[ForeignKey(nameof(ProductId))]
		public virtual ProductEntity? Product { get; set; }
	}
}
