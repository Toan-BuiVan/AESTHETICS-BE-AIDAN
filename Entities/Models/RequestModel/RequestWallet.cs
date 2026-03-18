using System;
using System.Collections.Generic;
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
}
