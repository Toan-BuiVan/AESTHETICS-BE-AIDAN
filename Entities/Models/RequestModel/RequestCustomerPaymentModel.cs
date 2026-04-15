using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Models.RequestModel
{
	public class CreateCustomerPaymentModel
	{
		public int? CustomerId { get; set; }

		/// <summary>Số tài khoản ngân hàng khách hàng</summary>
		[MaxLength(50)]
		public string? BankAccountNumber { get; set; }

		/// <summary>Tên chủ tài khoản</summary>
		[MaxLength(200)]
		public string? BankAccountName { get; set; }

		/// <summary>Tên ngân hàng (BIDV, VCB, ACB, MB, VP, etc)</summary>
		[MaxLength(100)]
		public string? BankName { get; set; }

		/// <summary>Mã ngân hàng (BIDV, VCB, ACB)</summary>
		[MaxLength(10)]
		public string? BankCode { get; set; }
	}

	public class updatecustomerpayment 
	{
		public int? Id { get; set; }
		public int? CustomerId { get; set; }

		/// <summary>Số tài khoản ngân hàng khách hàng</summary>
		[MaxLength(50)]
		public string? BankAccountNumber { get; set; }

		/// <summary>Tên chủ tài khoản</summary>
		[MaxLength(200)]
		public string? BankAccountName { get; set; }

		/// <summary>Tên ngân hàng (BIDV, VCB, ACB, MB, VP, etc)</summary>
		[MaxLength(100)]
		public string? BankName { get; set; }

		/// <summary>Mã ngân hàng (BIDV, VCB, ACB)</summary>
		[MaxLength(10)]
		public string? BankCode { get; set; }

		public bool IsDefault { get; set; }
	}

	public class deletecustomerpayment
	{
		public int? Id { get; set; }
	}

	public class getlistcustomerpayment : BaseSearchModel
	{
		public int? customerId { get; set; }
	}
}
