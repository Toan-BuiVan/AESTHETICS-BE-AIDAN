using Aesthetics.Entities.Models.RequestModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Data.AestheticsInterfaces
{
	public interface ICustomerService
	{
		Task<bool> UpdateCustomer(RequestUpdateCustomer request);
	}
}
