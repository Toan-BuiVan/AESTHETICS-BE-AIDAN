using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aesthetics.Entities.Enum;

namespace Aesthetics.Entities.Models.RequestModel
{
    public class CreateInvoice
    {
		public int? CustomerId { get; set; }

		/// <summary>FK → Staffs: nhân viên tạo hóa đơn</summary>
		public int? StaffId { get; set; }

		/// <summary>FK → Vouchers: voucher áp dụng (nếu có)</summary>
		public int? VoucherId { get; set; }

		// <summary>FK → TreatmentPlans: gói liệu trình</summary>
		public int? TreatmentPlanId { get; set; }

		/// <summary>FK → Services: dịch vụ</summary>
		public int? ServiceId { get; set; }

		/// <summary>Số tiền đã trả</summary>
		public decimal PaidAmount { get; set; }

		/// <summary>FK → Product: sản phẩm</summary>
		public int? ProductId { get; set; }

		public string? PaymentMethod { get; set; }
		public EnumTreatmentPlans? TypeInvoice { get; set; }
	}

	public class GetInvoice : BaseSearchModel
	{
		public int? CustomerId { get; set; }

		/// <summary>FK → Staffs: nhân viên tạo hóa đơn</summary>
		public int? StaffId { get; set; }
	}
}
