using Aesthetics.Entities.Entities;
using Aesthetics.Entities.Models.RequestModel;
using Aesthetics.Entities.Models.ResponseModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Data.AestheticsInterfaces
{
    public interface IWalletService
    {
        Task<bool> create(CreateWallet wallet);
		Task<bool> delete(DeleteWallest wallet);
		Task<BaseDataCollection<WalletEntity>> getlist(WalletGet clinic);
	}
}   
