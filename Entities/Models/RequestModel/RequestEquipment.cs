using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Models.RequestModel
{
    public class RequestEquipment
    {
		public string EquipmentName { get; set; }
		public int ClinicId { get; set; }
	}

	public class UpdateEquipment
	{
		public int Id { get; set; }
		public string? EquipmentName { get; set; }
		public int? ClinicId { get; set; }
		public string? Status { get; set; }
	}

	public class DeleteEquipment
	{
		public int Id { get; set; }
	}

	public class EquipmentGet : BaseSearchModel
	{
		public string? EquipmentName { get; set; }
		public int? ClinicId { get; set; }
		public int? ServiceTypeId { get; set; }
	}
}
