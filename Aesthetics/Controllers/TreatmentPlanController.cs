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
	public class TreatmentPlanController : ControllerBase
	{
		private readonly ITreatmentPlanService _treatmentPlanService;

		public TreatmentPlanController(ITreatmentPlanService treatmentPlanService)
		{
			_treatmentPlanService = treatmentPlanService;
		}

		[ServiceFilter(typeof(Filter_CheckToken))]
		[Filter_Authorization("createtreatmentplan")]
		[HttpPost("createtreatmentplan")]
		public async Task<IActionResult> Create([FromBody] CreateTreatmentPlan plan)
		{
			var result = await _treatmentPlanService.create(plan);
			return Ok(new { success = result });
		}

		[ServiceFilter(typeof(Filter_CheckToken))]
		[Filter_Authorization("updatetreatmentplan")]
		[HttpPost("updatetreatmentplan")]
		public async Task<IActionResult> Update([FromBody] UpdateTreatmentPlan plan)
		{
			var result = await _treatmentPlanService.update(plan);
			return Ok(new { success = result });
		}

		[ServiceFilter(typeof(Filter_CheckToken))]
		[Filter_Authorization("deletetreatmentplan")]
		[HttpPost("deletetreatmentplan")]
		public async Task<IActionResult> Delete([FromBody] DeleteTreatmentPlan plan)
		{
			var result = await _treatmentPlanService.delete(plan);
			return Ok(new { success = result });
		}

		[HttpPost("gettreatmentplanlist")]
		public async Task<IActionResult> GetList([FromBody] TreatmentPlanGet plan)
		{
			var result = await _treatmentPlanService.getlist(plan);
			return Ok(result);
		}
	}
}
