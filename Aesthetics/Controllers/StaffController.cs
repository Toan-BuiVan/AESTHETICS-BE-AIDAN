using Aesthetics.Data.AestheticsInterfaces;
using Aesthetics.Entities.Models.RequestModel;
using ASP_NetCore_Aesthetics.Filter;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static Aesthetics.Entities.Models.RequestModel.RequestUpdateStaff;

namespace Aesthetics.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class StaffController : ControllerBase
	{
		private readonly IStaffService _staffService;

		public StaffController(IStaffService staffService)
		{
			_staffService = staffService;
		}


		[HttpPost("getliststaff")]
		public async Task<IActionResult> GetList([FromBody] RequestStaffSearch searchRequest)
		{
			if (searchRequest == null)
			{
				return BadRequest(new { message = "Search request cannot be null" });
			}

			var result = await _staffService.GetListAsync(searchRequest);
			return Ok(result);
		}

		[ServiceFilter(typeof(Filter_CheckToken))]
		[Filter_Authorization("updatestaff")]
		[HttpPost("updatestaff")]
		public async Task<bool> updatestaff( UpdateStaffRequest searchRequest)
		{
			return await _staffService.UpdateStaff(searchRequest);
		}
	}
}
