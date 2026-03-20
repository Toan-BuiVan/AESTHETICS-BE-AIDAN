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
	public class ClinicController : ControllerBase
	{
		private readonly IClinicService _clinicService;

		public ClinicController(IClinicService clinicService)
		{
			_clinicService = clinicService;
		}

		[HttpPost("createclinic")]
		public async Task<IActionResult> Create([FromBody] RequestClinic clinic)
		{
			var result = await _clinicService.create(clinic);
			return Ok(new { success = result });
		}

		[HttpPost("updateclinic")]
		public async Task<IActionResult> Update([FromBody] UpdateClinic clinic)
		{
			var result = await _clinicService.update(clinic);
			return Ok(new { success = result });
		}

		[HttpPost("deleteclinic")]
		public async Task<IActionResult> Delete([FromBody] DeleteClinic clinic)
		{
			var result = await _clinicService.delete(clinic);
			return Ok(new { success = result });
		}

		[HttpPost("getclinielist")]
		public async Task<IActionResult> GetList([FromBody] ClinicGet clinic)
		{
			var result = await _clinicService.getlist(clinic);
			return Ok(result);
		}
	}
}
