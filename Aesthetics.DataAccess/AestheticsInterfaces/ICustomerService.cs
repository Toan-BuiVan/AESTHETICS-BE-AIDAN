using Aesthetics.Entities.Models.RequestModel;
using Aesthetics.Entities.Models.ResponseModel;
using Aesthetics.Entities.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Data.RepositoryInterfaces
{
	public interface ICustomerService
	{
		Task<bool> UpdateCustomer(RequestUpdateCustomer request);
		Task<BaseDataCollection<CustomerEntity>> GetListCustomer(RequestCustomer request);
	}
}
