using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Models.RequestModel
{
	public class RequestUpdateCustomer
	{
		public int AccountId { get; set; }

		/// <summary>Họ tên khách hàng</summary>
		public string? FullName { get; set; }

		/// <summary>Ngày sinh</summary>
		public DateTime? DateBirth { get; set; }

		/// <summary>Giới tính: 'Nam', 'Nu', 'Khac'</summary>
		public string? Sex { get; set; }

		/// <summary>Số điện thoại</summary>
		public string? Phone { get; set; }

		/// <summary>Địa chỉ</summary>
		public string? Address { get; set; }

		/// <summary>Email</summary>
		public string? Email { get; set; }

		/// <summary>Số CCCD/CMND</summary>
		public string? IDCard { get; set; }
	}
}
