using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Enum
{
	public enum EnumRankMember
	{
		Bronze = 1,
		Silver = 2,
		Gold = 3,
		Diamond = 4
	}
	public static class RankHelper
	{
		public static bool CanUseVoucher(EnumRankMember customerRank, EnumRankMember voucherRank)
		{
			return (int)voucherRank <= (int)customerRank;
		}

		public static EnumRankMember? ParseRank(string? rankString)
		{
			if (string.IsNullOrWhiteSpace(rankString))
				return null;

			if (System.Enum.TryParse<EnumRankMember>(rankString.Trim(), ignoreCase: true, out var rank))
				return rank;

			return null;
		}
	}
}
