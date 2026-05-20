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
	public class EquipmentController : ControllerBase
	{
		private readonly IEquipmentService _equipmentService;

		public EquipmentController(IEquipmentService equipmentService)
		{
			_equipmentService = equipmentService;
		}

		[ServiceFilter(typeof(Filter_CheckToken))]
		[Filter_Authorization("createequipment")]
		[HttpPost("createequipment")]
		public async Task<IActionResult> Create([FromBody] RequestEquipment equipment)
		{
			var result = await _equipmentService.create(equipment);
			return Ok(new { success = result });
		}

		[ServiceFilter(typeof(Filter_CheckToken))]
		[Filter_Authorization("updateequipment")]
		[HttpPost("updateequipment")]
		public async Task<IActionResult> Update([FromBody] UpdateEquipment equipment)
		{
			var result = await _equipmentService.update(equipment);
			return Ok(new { success = result });
		}

		[ServiceFilter(typeof(Filter_CheckToken))]
		[Filter_Authorization("deleteequipment")]
		[HttpPost("deleteequipment")]
		public async Task<IActionResult> Delete([FromBody] DeleteEquipment equipment)
		{
			var result = await _equipmentService.delete(equipment);
			return Ok(new { success = result });
		}

		[HttpPost("getequipmentlist")]
		public async Task<IActionResult> GetList([FromBody] EquipmentGet equipment)
		{
			var result = await _equipmentService.getlist(equipment);
			return Ok(result);
		}
	}
}
