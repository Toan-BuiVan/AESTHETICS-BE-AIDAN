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
	public class ServiceController : ControllerBase
	{
		private readonly IServicesService _servicesService;

		public ServiceController(IServicesService servicesService)
		{
			_servicesService = servicesService;
		}

		[ServiceFilter(typeof(Filter_CheckToken))]
		[Filter_Authorization("createservice")]
		[HttpPost("createservice")]
		public async Task<IActionResult> Create([FromBody] CreateService service)
		{
			var result = await _servicesService.create(service);
			return Ok(new { success = result });
		}

		[ServiceFilter(typeof(Filter_CheckToken))]
		[Filter_Authorization("updateservice")]
		[HttpPost("updateservice")]
		public async Task<IActionResult> Update([FromBody] UpdateService service)
		{
			var result = await _servicesService.update(service);
			return Ok(new { success = result });
		}

		[ServiceFilter(typeof(Filter_CheckToken))]
		[Filter_Authorization("deleteservice")]
		[HttpPost("deleteservice")]
		public async Task<IActionResult> Delete([FromBody] DeleteService service)
		{
			var result = await _servicesService.delete(service);
			return Ok(new { success = result });
		}

		[HttpPost("getservicelist")]
		public async Task<IActionResult> GetList([FromBody] ServiceGet service)
		{
			var result = await _servicesService.GetListAsync(service);
			return Ok(result);
		}

		[ServiceFilter(typeof(Filter_CheckToken))]
		[Filter_Authorization("exportservicetoexcel")]
		[HttpPost("exportservicetoexcel")]
		public async Task<IActionResult> ExportToExcel([FromBody] exportservice service)
		{
			var result = await _servicesService.ExportToExcelAsync(service);
			return File(result, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Services.xlsx");
		}
	}
}
