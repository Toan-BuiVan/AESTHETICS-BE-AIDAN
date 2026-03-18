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
	public interface IServiceTypeService
    {
		Task<bool> create(RequestServiceType serviceType);

		Task<bool> update(UpdateServiceType serviceType);

		Task<bool> delete(DeleteServiceType serviceType);

		Task<BaseDataCollection<ServiceTypeEntity>> getlist(ServiceTypeGet serviceType);
	}
}
