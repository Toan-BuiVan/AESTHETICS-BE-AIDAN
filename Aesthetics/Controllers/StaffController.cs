using Aesthetics.Data.AestheticsInterfaces;
using Aesthetics.Entities.Models.RequestModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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

		[HttpPost("get-list")]
		public async Task<IActionResult> GetList([FromBody] RequestStaffSearch searchRequest)
		{
			if (searchRequest == null)
			{
				return BadRequest(new { message = "Search request cannot be null" });
			}

			var result = await _staffService.GetListAsync(searchRequest);
			return Ok(result);
		}
	}
}
