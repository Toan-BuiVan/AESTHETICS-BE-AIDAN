using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Models.ResponseModel
{
	public class WalletResponseModel
	{
		public int Id { get; set; }
		public int? CustomerId { get; set; }
		public int? VoucherId { get; set; }
		public DateTime? ClaimedDate { get; set; }
		public bool IsUsed { get; set; }
		public string? VoucherCode { get; set; }
		public string? VoucherDescription { get; set; }
		public string? VoucherImage { get; set; }
		public decimal? DiscountValue { get; set; }
		public DateTime? StartDate { get; set; }
		public DateTime? EndDate { get; set; }
		public decimal? MinimumOrderValue { get; set; }
		public decimal? MaxValue { get; set; }
		public string? RankMember { get; set; }
		public bool IsActive { get; set; }
	}
}
