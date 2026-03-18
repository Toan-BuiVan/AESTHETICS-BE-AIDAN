using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Entities
{
	[Table("SessionProducts")]
	public class SessionProductEntity : Aesthetics.Entities.BaseEntity.BaseEntity
	{
		/// <summary>FK → TreatmentSessions: buổi nào</summary>
		public int? TreatmentSessionId { get; set; }

		/// <summary>FK → Products: sản phẩm nào</summary>
		public int? ProductId { get; set; }

		/// <summary>Số lượng sản phẩm dùng cho buổi này</summary>
		public int? QuantityUsed { get; set; }

		public int? ServiceId { get; set; }

		// Navigation properties
		[ForeignKey(nameof(TreatmentSessionId))]
		public virtual TreatmentSessionEntity? TreatmentSession { get; set; }

		[ForeignKey(nameof(ProductId))]
		public virtual ProductEntity? Product { get; set; }
	}
}
