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
    public interface IServicesService
    {
		Task<CreateServiceResponseModel> create(CreateService service);

		Task<bool> update(UpdateService service);

		Task<bool> delete(DeleteService service);

		Task<byte[]> ExportToExcelAsync(exportservice service);

		Task<BaseDataCollection<ServiceResponseModel>> GetListAsync(ServiceGet service);
	}
}
