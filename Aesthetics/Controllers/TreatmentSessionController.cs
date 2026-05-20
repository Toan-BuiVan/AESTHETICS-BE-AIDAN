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
	public class TreatmentSessionController : ControllerBase
	{
		private readonly ITreatmentSessionService _treatmentSessionService;

		public TreatmentSessionController(ITreatmentSessionService treatmentSessionService)
		{
			_treatmentSessionService = treatmentSessionService;
		}

		[ServiceFilter(typeof(Filter_CheckToken))]
		[Filter_Authorization("createtreatmentsession")]
		[HttpPost("createtreatmentsession")]
		public async Task<IActionResult> Create([FromBody] CreateTreatmentSession session)
		{
			var result = await _treatmentSessionService.create(session);
			return Ok(new { success = result });
		}

		[ServiceFilter(typeof(Filter_CheckToken))]
		[Filter_Authorization("updatetreatmentsession")]
		[HttpPost("updatetreatmentsession")]
		public async Task<IActionResult> Update([FromBody] UpdateTreatmentSession session)
		{
			var result = await _treatmentSessionService.update(session);
			return Ok(new { success = result });
		}

		[ServiceFilter(typeof(Filter_CheckToken))]
		[Filter_Authorization("deletetreatmentsession")]
		[HttpPost("deletetreatmentsession")]
		public async Task<IActionResult> Delete([FromBody] DeleteTreatmentSession session)
		{
			var result = await _treatmentSessionService.delete(session);
			return Ok(new { success = result });
		}

		[HttpPost("gettreatmentsessionlist")]
		public async Task<IActionResult> GetList([FromBody] TreatmentSessionGet session)
		{
			var result = await _treatmentSessionService.getlist(session);
			return Ok(result);
		}
	}
}
