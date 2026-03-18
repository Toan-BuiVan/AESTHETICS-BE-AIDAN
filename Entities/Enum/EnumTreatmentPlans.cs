using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Enum
{
	public enum EnumTreatmentPlans
	{
		PayAfterService = 0,   // Trả sau (làm xong mới thanh toán)
		PayInAdvance = 1,      // Trả trước toàn bộ
		PartialPayment = 2     // Trả một phần (đặt cọc trước)
	}
}
