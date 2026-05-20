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
	public class CustomerTreatmentSessionsController : ControllerBase
	{
		private readonly ICustomerTreatmentSessionsService _customerTreatmentSessionsService;

		public CustomerTreatmentSessionsController(ICustomerTreatmentSessionsService customerTreatmentSessionsService)
		{
			_customerTreatmentSessionsService = customerTreatmentSessionsService;
		}

		[ServiceFilter(typeof(Filter_CheckToken))]
		[Filter_Authorization("updatecustomertreatmentsession")]
		[HttpPost("updatecustomertreatmentsession")]
		public async Task<IActionResult> Update([FromBody] UpdateCustomerTreatmentSessions requestCustomer)
		{
			var result = await _customerTreatmentSessionsService.update(requestCustomer);
			return Ok(new { success = result });
		}

		[ServiceFilter(typeof(Filter_CheckToken))]
		[Filter_Authorization("deletecustomertreatmentsession")]
		[HttpPost("deletecustomertreatmentsession")]
		public async Task<IActionResult> Delete([FromBody] DeleteCustomerTreatmentSessions requestCustomer)
		{
			var result = await _customerTreatmentSessionsService.delete(requestCustomer);
			return Ok(new { success = result });
		}
	}
}
