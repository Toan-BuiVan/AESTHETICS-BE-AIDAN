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
	public class AppointmentTimeLockController : ControllerBase
	{
		private readonly IAppointmentTimeLockService _appointmentTimeLockService;

		public AppointmentTimeLockController(IAppointmentTimeLockService appointmentTimeLockService)
		{
			_appointmentTimeLockService = appointmentTimeLockService;
		}

		[ServiceFilter(typeof(Filter_CheckToken))]
		[Filter_Authorization("createappointmenttimelock")]
		[HttpPost("createappointmenttimelock")]
		public async Task<IActionResult> Create([FromBody] CreateAppointmentTimeLock timeLock)
		{
			var result = await _appointmentTimeLockService.create(timeLock);
			return Ok(new { success = result });
		}

		[ServiceFilter(typeof(Filter_CheckToken))]
		[Filter_Authorization("updateappointmenttimelock")]
		[HttpPost("updateappointmenttimelock")]
		public async Task<IActionResult> Update([FromBody] UpdateAppointmentTimeLock timeLock)
		{
			var result = await _appointmentTimeLockService.update(timeLock);
			return Ok(new { success = result });
		}

		[ServiceFilter(typeof(Filter_CheckToken))]
		[Filter_Authorization("deleteappointmenttimelock")]
		[HttpPost("deleteappointmenttimelock")]
		public async Task<IActionResult> Delete([FromBody] DeleteAppointmentTimeLock timeLock)
		{
			var result = await _appointmentTimeLockService.delete(timeLock);
			return Ok(new { success = result });
		}

		//[ServiceFilter(typeof(Filter_CheckToken))]
		//[Filter_Authorization("getappointmenttimelockList")]
		[HttpPost("getappointmenttimelockList")]
		public async Task<IActionResult> GetList([FromBody] GetAppointmentTimeLock timeLock)
		{
			var result = await _appointmentTimeLockService.getlist(timeLock);
			return Ok(result);
		}
	}
}
