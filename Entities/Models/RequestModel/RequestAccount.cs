using Aesthetics.Entities.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Models.RequestModel
{
    public class RequestAccount
    {
		public string? UserName { get; set; }
		public string? PassWord { get; set; }
		public string? ReferralCode { get; set; }
		public AccountRole? AccountType { get; set; }
		public bool? IsDoctor { get; set; }
	}

	public class UpdateAccount
	{
		public int Id { get; set; }
		public string? OriginPassWord { get; set; }
		public string? NewPassWord { get; set; }
	}

	public class DeleteAccount
	{
		public int Id { get; set; }
	}

	public class AccountGet : BaseSearchModel
	{
		public int? Id { get; set; }
		public string? UserName { get; set; }
	}
}
