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
	public class ClinicStaffController : ControllerBase
	{
		private readonly IClinicStaffService _clinicStaffService;

		public ClinicStaffController(IClinicStaffService clinicStaffService)
		{
			_clinicStaffService = clinicStaffService;
		}

		[HttpPost("createclinicstaff")]
		public async Task<IActionResult> Create([FromBody] CreateClinicStaff clinicStaff)
		{
			var result = await _clinicStaffService.create(clinicStaff);
			return Ok(new { success = result });
		}

		[HttpPost("updateclinicstaff")]
		public async Task<IActionResult> Update([FromBody] UpdateClinicStaff clinicStaff)
		{
			var result = await _clinicStaffService.update(clinicStaff);
			return Ok(new { success = result });
		}

		[HttpPost("deleteclinicstaff")]
		public async Task<IActionResult> Delete([FromBody] DeleteClinicStaff clinicStaff)
		{
			var result = await _clinicStaffService.delete(clinicStaff);
			return Ok(new { success = result });
		}

		[HttpPost("getclinicstafflist")]
		public async Task<IActionResult> GetList([FromBody] GetClinicStaff clinicStaff)
		{
			var result = await _clinicStaffService.getlist(clinicStaff);
			return Ok(result);
		}
	}
}
