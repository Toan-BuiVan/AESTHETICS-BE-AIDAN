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
	public class StaffShiftController : ControllerBase
	{
		private readonly IStaffShiftServices _staffShiftService;

		public StaffShiftController(IStaffShiftServices staffShiftService)
		{
			_staffShiftService = staffShiftService;
		}

		[ServiceFilter(typeof(Filter_CheckToken))]
		[Filter_Authorization("createstaffshift")]
		[HttpPost("createstaffshift")]
		public async Task<IActionResult> Create([FromBody] CreateStaffShift staffShift)
		{
			var result = await _staffShiftService.create(staffShift);
			return Ok(new { success = result });
		}

		[ServiceFilter(typeof(Filter_CheckToken))]
		[Filter_Authorization("deletestaffshift")]
		[HttpPost("deletestaffshift")]
		public async Task<IActionResult> Delete([FromBody] DeleteStaffShift staffShift)
		{
			var result = await _staffShiftService.delete(staffShift);
			return Ok(new { success = result });
		}

		//[ServiceFilter(typeof(Filter_CheckToken))]
		//[Filter_Authorization("getstaffshiftlist")]
		[HttpPost("getstaffshiftlist")]
		public async Task<IActionResult> GetList([FromBody] GetStaffShift staffShift)
		{
			var result = await _staffShiftService.getlist(staffShift);
			return Ok(result);
		}
	}
}
