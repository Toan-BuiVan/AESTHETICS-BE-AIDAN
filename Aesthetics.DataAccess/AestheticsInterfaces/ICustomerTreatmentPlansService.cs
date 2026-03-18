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
    public interface ICustomerTreatmentPlansService
    {
		Task<bool> create(CreateCustomerTreatment treatment);

		Task<bool> delete(DeleteCustomerTreatment treatment);

		Task<BaseDataCollection<CustomerTreatmentPlanEntity>> getlist(GetCustomerTreatment treatment);
		Task<bool> update(UpdateCustomerTreatment request);
	}
}
