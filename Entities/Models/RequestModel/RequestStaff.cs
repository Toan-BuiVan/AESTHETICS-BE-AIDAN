using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Models.RequestModel
{
	public class RequestStaffSearch : BaseSearchModel
	{
		/// <summary>Lọc theo bác sĩ hay không</summary>
		public bool? IsDoctor { get; set; }

		/// <summary>Lọc theo phòng khám</summary>
		public int? ClinicId { get; set; }

		/// <summary>Lọc theo phòng khám</summary>
		public int? ServicetypeId { get; set; }
	}
}
