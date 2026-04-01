using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Models.RequestModel
{
    public class CreateWallet
    {
		public int CustomerId { get; set; }
		public int VoucherId { get; set; }
	}
	public class RedeemVouchers
	{
		public int CustomerId { get; set; }
		public int VoucherId { get; set; }
		public string PointType { get; set; }
	}

	public class DeleteWallest
	{
		public int WalletsID { get; set; }
	}

	public class WalletGet : BaseSearchModel
	{
		public int CustomerId { get; set; }
	}

	/// <summary>
	/// Request để đổi voucher bằng điểm
	/// </summary>
	public class RequestExchangeVoucher
	{
		/// <summary>ID khách hàng</summary>
		[Required]
		public int CustomerId { get; set; }

		/// <summary>ID voucher muốn đổi</summary>
		[Required]
		public int VoucherId { get; set; }

		/// <summary>
		/// Loại điểm dùng để đổi
		/// 0 = AccumulatedPoints (điểm giới thiệu)
		/// 1 = RatingPoints (điểm mua hàng)
		/// </summary>
		[Required]
		[Range(0, 1)]
		public int PointType { get; set; }
	}
}
