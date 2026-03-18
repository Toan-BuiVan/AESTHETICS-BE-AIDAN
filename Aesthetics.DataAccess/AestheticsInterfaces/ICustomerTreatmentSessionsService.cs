using Aesthetics.Entities.Models.RequestModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Data.AestheticsInterfaces
{
    public interface ICustomerTreatmentSessionsService
    {
		Task<bool> update(UpdateCustomerTreatmentSessions requestCustomer);
		Task<bool> delete(DeleteCustomerTreatmentSessions requestCustomer);
	}
}
