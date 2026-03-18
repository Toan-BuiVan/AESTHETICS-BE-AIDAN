using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Models.RequestModel
{
    public class UpdateCustomerTreatmentSessions
    {
		public int? Id { get; set; }
		/// <summary>
		/// ChuaThucHien: chưa đến lượt,
		/// ChoDatLich: Chờ đặt lịch khám,
		/// DaDatLich: đã đặt lịch hẹn,
		/// DangThucHien: đang thực hiện,
		/// HoanThanh: buổi này đã xong,
		/// BoLo: khách không đến (no-show)
		/// </summary>
		/// 

		public string? Status { get; set; }
	}

	public class DeleteCustomerTreatmentSessions
	{
		public int? Id { get; set; }
	}
}
