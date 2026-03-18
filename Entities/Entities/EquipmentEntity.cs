using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Entities
{
	[Table("Equipments")]
	public class EquipmentEntity : Aesthetics.Entities.BaseEntity.BaseEntity
	{
		/// <summary>Tên thiết bị: 'Máy Laser Picosure', 'Máy RF'</summary>
		[MaxLength(250)]
		public string? EquipmentName { get; set; }

		/// <summary>FK → Clinics: thuộc phòng nào</summary>
		public int? ClinicId { get; set; }

		/// <summary>SanSang, DangSuDung, HuHong, BaoTri</summary>
		[MaxLength(20)]
		public string? Status { get; set; }

		// Navigation properties
		[ForeignKey(nameof(ClinicId))]
		public virtual ClinicEntity? Clinic { get; set; }

		public virtual ICollection<AppointmentAssignmentEntity> AppointmentAssignments { get; set; } = [];
	}
}
