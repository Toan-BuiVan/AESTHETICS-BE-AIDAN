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
	public class AppointmentController : ControllerBase
	{
		private readonly IAppointmentService _appointmentService;

		public AppointmentController(IAppointmentService appointmentService)
		{
			_appointmentService = appointmentService;
		}

		[ServiceFilter(typeof(Filter_CheckToken))]
		[Filter_Authorization("createappointment")]
		[HttpPost("createappointment")]
		public async Task<IActionResult> Create([FromBody] CreateAppointment appointment)
		{
			var result = await _appointmentService.create(appointment);
			return Ok(new { success = result });
		}

		[ServiceFilter(typeof(Filter_CheckToken))]
		[Filter_Authorization("updateappointmentstatus")]
		[HttpPost("updateappointmentstatus")]
		public async Task<IActionResult> updateappointmentstatusasync([FromBody] updateappoint appointment)
		{
			var result = await _appointmentService.UpdateAppointmentStatusAsync(appointment);
			return Ok(new { success = result });
		}

		[ServiceFilter(typeof(Filter_CheckToken))]
		[Filter_Authorization("deleteappointment")]
		[HttpPost("deleteappointment")]
		public async Task<IActionResult> Delete([FromBody] DeleteAppointment appointment)
		{
			var result = await _appointmentService.delete(appointment);
			return Ok(new { success = result });
		}

		//[ServiceFilter(typeof(Filter_CheckToken))]
		//[Filter_Authorization("getappointmentlist")]
		[HttpPost("getappointmentlist")]
		public async Task<IActionResult> GetList([FromBody] AppointmentGet appointment)
		{
			var result = await _appointmentService.getlist(appointment);
			return Ok(result);
		}

		//[ServiceFilter(typeof(Filter_CheckToken))]
		//[Filter_Authorization("getdoctoravailability")]
		[HttpPost("getdoctoravailability")]
		public async Task<IActionResult> getdoctoravailability([FromBody] GetDoctorAvailabilityRequest appointment)
		{
			var result = await _appointmentService.GetDoctorAvailability(appointment);
			return Ok(result);
		}

		//[ServiceFilter(typeof(Filter_CheckToken))]
		//[Filter_Authorization("createappointment")]
		[HttpGet("getdoctorservices/{doctorId}")]
		public async Task<IActionResult> GetDoctorServices(int doctorId)
		{
			var result = await _appointmentService.GetDoctorServices(doctorId);
			if (result == null || result.Count == 0)
			{
				return Ok(new
				{
					success = false,
					message = "Bác sĩ không có dịch vụ nào hoặc không tồn tại",
					data = new List<object>()
				});
			}

			return Ok(new
			{
				success = true,
				message = $"Tìm thấy {result.Count} dịch vụ",
				totalServices = result.Count,
				data = result
			});
		}
	}
}
