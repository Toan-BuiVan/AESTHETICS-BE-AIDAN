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
	public interface ICustomerPaymentInfoService
	{
		public Task<bool> createcustomerpayment(CreateCustomerPaymentModel model);
		public Task<bool> updatecustomerpayment(updatecustomerpayment model);
		public Task<bool> deletecustomerpayment(deletecustomerpayment model);
		public Task<BaseDataCollection<CustomerPaymentInfoEntity>> getlistcustomerpayment(getlistcustomerpayment model);
	}
}
