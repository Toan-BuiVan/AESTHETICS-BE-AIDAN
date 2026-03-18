using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Models.RequestModel
{
    public class CreateVoucher
    {
		public string? Description { get; set; }

		public string? VoucherImage { get; set; }

		public decimal DiscountValue { get; set; } = 0;

		public DateTime StartDate { get; set; }

		public DateTime EndDate { get; set; }

		public decimal MinimumOrderValue { get; set; } = 0;

		public decimal MaxValue { get; set; } = 0;

		[StringLength(200)]
		public string? RankMember { get; set; }

		public int RatingPoints { get; set; } = 0;

		public int AccumulatedPoints { get; set; } = 0;

		public int UsageLimit { get; set; } = 0;
	}

	public class UpdateVoucher
	{
		public int Id { get; set; }
		public string? Description { get; set; }

		public string? VoucherImage { get; set; }

		public decimal? DiscountValue { get; set; } = 0;

		public DateTime? StartDate { get; set; }

		public DateTime? EndDate { get; set; }

		public decimal? MinimumOrderValue { get; set; } = 0;

		public decimal? MaxValue { get; set; } = 0;

		[StringLength(200)]
		public string? RankMember { get; set; }

		public int? RatingPoints { get; set; } = 0;

		public int? AccumulatedPoints { get; set; } = 0;

		public int? UsageLimit { get; set; } = 0;

		public bool? IsActive { get; set; } = false;
	}

	public class DeleteVoucher
	{
		public int Id { get; set; }
	}

	public class VoucherGet : BaseSearchModel
	{
		public string? Code { get; set; }
		public DateTime? StartDate { get; set; }
		public DateTime? EndDate { get; set; }
		public string? RankMember { get; set; }

	}
}
