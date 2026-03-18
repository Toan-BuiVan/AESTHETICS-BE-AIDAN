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
    public interface ISupplierSevice
    {
		Task<bool> create(RequestSupplier supplier);

		Task<bool> update(SupplierUpdate supplier);

		Task<bool> delete(SupplierDelete supplier);

		Task<BaseDataCollection<SupplierEntity>> getlist(SupplierGet searchSupplier);
	}
}
