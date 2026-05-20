using Aesthetics.Data.AestheticsInterfaces;
using Aesthetics.Data.RepositoryInterfaces;
using Aesthetics.Data.RepositoryServices;
using Aesthetics.Entities.Entities;
using Aesthetics.Entities.Models.RequestModel;
using Aesthetics.Entities.Models.ResponseModel;
using ASP_NetCore_Aesthetics.Filter;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;

namespace Aesthetics.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SupplierController : ControllerBase
    {
		private ISupplierSevice _supplierSevice;
		public SupplierController(ISupplierSevice supplierSevice)
		{
			_supplierSevice = supplierSevice;
		}

		[ServiceFilter(typeof(Filter_CheckToken))]
		[Filter_Authorization("createsupplier")]
		[HttpPost("createsupplier")]
		public async Task<bool> create(RequestSupplier supplier)
		{
			return await _supplierSevice.create(supplier);
		}

		[ServiceFilter(typeof(Filter_CheckToken))]
		[Filter_Authorization("updatesupplier")]
		[HttpPost("updatesupplier")]
		public async Task<bool> update(SupplierUpdate supplier)
		{
			return await _supplierSevice.update(supplier);
		}

		[ServiceFilter(typeof(Filter_CheckToken))]
		[Filter_Authorization("deletesupplier")]
		[HttpPost("deletesupplier")]
		public async Task<bool> detele(SupplierDelete supplier)
		{
			return await _supplierSevice.delete(supplier);
		}

		[HttpPost("pagingsupplier")]
		public async Task<BaseDataCollection<SupplierEntity>> paging(SupplierGet supplier)
		{
			return await _supplierSevice.getlist(supplier);
		}
	}
}
