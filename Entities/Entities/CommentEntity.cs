using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Entities
{
	[Table("Comments")]
	public class CommentEntity : Aesthetics.Entities.BaseEntity.BaseEntity
	{
		/// <summary>FK → Products: đánh giá sản phẩm (null nếu đánh giá dịch vụ)</summary>
		public int? ProductId { get; set; }

		/// <summary>FK → Services: đánh giá dịch vụ (null nếu đánh giá sản phẩm)</summary>
		public int? ServiceId { get; set; }

		/// <summary>FK → Customers: khách hàng viết đánh giá</summary>
		public int? CustomerId { get; set; }

		/// <summary>Nội dung bình luận</summary>
		public string? CommentContent { get; set; }

		/// <summary>Đánh giá sao: 1 đến 5</summary>
		[Range(1, 5)]
		public int? Rating { get; set; }

		/// <summary>Ngày tạo bình luận</summary>
		public DateTime? CreationDate { get; set; }

		// Navigation properties
		[ForeignKey(nameof(ProductId))]
		public virtual ProductEntity? Product { get; set; }

		[ForeignKey(nameof(ServiceId))]
		public virtual ServiceEntity? Service { get; set; }

		[ForeignKey(nameof(CustomerId))]
		public virtual CustomerEntity? Customer { get; set; }
	}
}
