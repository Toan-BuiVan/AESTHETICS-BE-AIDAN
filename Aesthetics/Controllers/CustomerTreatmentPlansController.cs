using Aesthetics.Data.AestheticsInterfaces;
using Aesthetics.Entities.Entities;
using Aesthetics.Entities.Models.RequestModel;
using Aesthetics.Entities.Models.ResponseModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aesthetics.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class CustomerTreatmentPlansController : ControllerBase
	{
		private readonly ICustomerTreatmentPlansService _customerTreatmentPlansService;

		public CustomerTreatmentPlansController(ICustomerTreatmentPlansService customerTreatmentPlansService)
		{
			_customerTreatmentPlansService = customerTreatmentPlansService;
		}

		[HttpPost("createcustomertreatmentplan")]
		public async Task<IActionResult> Create([FromBody] CreateCustomerTreatment treatment)
		{
			var result = await _customerTreatmentPlansService.create(treatment);
			return Ok( result );
		}

		[HttpPost("updatecustomertreatmentplan")]
		public async Task<IActionResult> Update([FromBody] UpdateCustomerTreatment treatment)
		{
			var result = await _customerTreatmentPlansService.update(treatment);
			return Ok(new { success = result });
		}

		[HttpPost("deletecustomertreatmentplan")]
		public async Task<IActionResult> Delete([FromBody] DeleteCustomerTreatment treatment)
		{
			var result = await _customerTreatmentPlansService.delete(treatment);
			return Ok(new { success = result });
		}

		[HttpPost("getcustomertreatmentplanlist")]
		public async Task<IActionResult> GetList([FromBody] GetCustomerTreatment treatment)
		{
			var result = await _customerTreatmentPlansService.getlist(treatment);
			return Ok(result);
		}
	}
}
