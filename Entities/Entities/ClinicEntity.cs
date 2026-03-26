using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Entities
{
	[Table("Clinics")]
	public class ClinicEntity : Aesthetics.Entities.BaseEntity.BaseEntity
	{
		/// <summary>Phòng khám dịch vụ nào</summary>
		public int? ServiceTypeId { get; set; }
		/// <summary>Tên phòng: 'Phòng Laser 1', 'Phòng Massage VIP'</summary>
		[MaxLength(255)]
		public string? ClinicName { get; set; }

		/// <summary>true = Đang mở, false = Đóng cửa</summary>
		public bool ClinicStatus { get; set; } = true;

		// Navigation properties
		public virtual ServiceTypeEntity? ServiceType { get; set; }
		public ICollection<AppointmentTimeLockEntity>? AppointmentTimeLocks { get; set; }
		public virtual ICollection<ClinicStaffEntity> ClinicStaffs { get; set; } = [];
		public virtual ICollection<EquipmentEntity> Equipments { get; set; } = [];
		public virtual ICollection<AppointmentAssignmentEntity> AppointmentAssignments { get; set; } = [];
	}
}
