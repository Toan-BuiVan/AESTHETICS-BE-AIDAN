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
	public class AppointmentController : ControllerBase
	{
		private readonly IAppointmentService _appointmentService;

		public AppointmentController(IAppointmentService appointmentService)
		{
			_appointmentService = appointmentService;
		}

		[HttpPost("createappointment")]
		public async Task<IActionResult> Create([FromBody] CreateAppointment appointment)
		{
			var result = await _appointmentService.create(appointment);
			return Ok(new { success = result });
		}

		[HttpPost("updateappointmentstatus")]
		public async Task<IActionResult> updateappointmentstatusasync([FromBody] updateappoint appointment)
		{
			var result = await _appointmentService.UpdateAppointmentStatusAsync(appointment);
			return Ok(new { success = result });
		}

		[HttpPost("deleteappointment")]
		public async Task<IActionResult> Delete([FromBody] DeleteAppointment appointment)
		{
			var result = await _appointmentService.delete(appointment);
			return Ok(new { success = result });
		}

		[HttpPost("getappointmentlist")]
		public async Task<IActionResult> GetList([FromBody] AppointmentGet appointment)
		{
			var result = await _appointmentService.getlist(appointment);
			return Ok(result);
		}

		[HttpPost("getdoctoravailability")]
		public async Task<IActionResult> getdoctoravailability([FromBody] GetDoctorAvailabilityRequest appointment)
		{
			var result = await _appointmentService.GetDoctorAvailability(appointment);
			return Ok(result);
		}
	}
}
