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
    public interface IAccountService
    {
		Task<bool> create(RequestAccount account);

		Task<bool> update(UpdateAccount account);

		Task<bool> delete(DeleteAccount account);

		Task<BaseDataCollection<AccountEntity>> getlist(AccountGet account);
		Task<AccountProfileResponseModel?> GetProfileByAccountIdAsync(int accountId);
	}
}
