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
	public class EquipmentController : ControllerBase
	{
		private readonly IEquipmentService _equipmentService;

		public EquipmentController(IEquipmentService equipmentService)
		{
			_equipmentService = equipmentService;
		}

		[HttpPost("createequipment")]
		public async Task<IActionResult> Create([FromBody] RequestEquipment equipment)
		{
			var result = await _equipmentService.create(equipment);
			return Ok(new { success = result });
		}

		[HttpPost("updateequipment")]
		public async Task<IActionResult> Update([FromBody] UpdateEquipment equipment)
		{
			var result = await _equipmentService.update(equipment);
			return Ok(new { success = result });
		}

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
