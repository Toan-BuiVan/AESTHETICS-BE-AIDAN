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
	public class InventoryAlertController : ControllerBase
	{
		private readonly IInventoryAlertService _inventoryAlertService;

		public InventoryAlertController(IInventoryAlertService inventoryAlertService)
		{
			_inventoryAlertService = inventoryAlertService;
		}

		[ServiceFilter(typeof(Filter_CheckToken))]
		[Filter_Authorization("createinventoryalert")]
		[HttpPost("createinventoryalert")]
		public async Task<IActionResult> CreateLowStockAlert(int productId, int currentQuantity, int minimumStock)
		{
			var result = await _inventoryAlertService.CreateLowStockAlert(productId, currentQuantity, minimumStock);
			return Ok(new { success = result });
		}

		[ServiceFilter(typeof(Filter_CheckToken))]
		[Filter_Authorization("sendinventoryalertemails")]
		[HttpPost("sendinventoryalertemails")]
		public async Task<IActionResult> SendPendingAlertEmails()
		{
			var result = await _inventoryAlertService.SendPendingAlertEmails();
			return Ok(new { success = result });
		}

		[ServiceFilter(typeof(Filter_CheckToken))]
		[Filter_Authorization("cleanupoldinventoryalerts")]
		[HttpPost("cleanupoldinventoryalerts")]
		public async Task<IActionResult> CleanupOldAlerts()
		{
			var result = await _inventoryAlertService.CleanupOldAlerts();
			return Ok(new { success = result });
		}
	}
}
