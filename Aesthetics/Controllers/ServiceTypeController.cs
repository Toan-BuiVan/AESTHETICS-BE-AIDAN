using Aesthetics.Data.AestheticsInterfaces;
using Aesthetics.Entities.Entities;
using Aesthetics.Entities.Models.RequestModel;
using Aesthetics.Entities.Models.ResponseModel;
using ASP_NetCore_Aesthetics.Filter;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aesthetics.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class ServiceTypeController : ControllerBase
	{
		private readonly IServiceTypeService _serviceTypeService;

		public ServiceTypeController(IServiceTypeService serviceTypeService)
		{
			_serviceTypeService = serviceTypeService;
		}

		[ServiceFilter(typeof(Filter_CheckToken))]
		[Filter_Authorization("createservicetype")]
		[HttpPost("createservicetype")]
		public async Task<IActionResult> Create([FromBody] RequestServiceType serviceType)
		{
			var result = await _serviceTypeService.create(serviceType);
			return Ok(new { success = result });
		}

		[ServiceFilter(typeof(Filter_CheckToken))]
		[Filter_Authorization("updateservicetype")]
		[HttpPost("updateservicetype")]
		public async Task<IActionResult> Update([FromBody] UpdateServiceType serviceType)
		{
			var result = await _serviceTypeService.update(serviceType);
			return Ok(new { success = result });
		}

		[ServiceFilter(typeof(Filter_CheckToken))]
		[Filter_Authorization("deleteservicetype")]
		[HttpPost("deleteservicetype")]
		public async Task<IActionResult> Delete([FromBody] DeleteServiceType serviceType)
		{
			var result = await _serviceTypeService.delete(serviceType);
			return Ok(new { success = result });
		}

		[HttpPost("getservicetypelist")]
		public async Task<IActionResult> GetList([FromBody] ServiceTypeGet serviceType)
		{
			var result = await _serviceTypeService.getlist(serviceType);
			return Ok(result);
		}
	}
}
