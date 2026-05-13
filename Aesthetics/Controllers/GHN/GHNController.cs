using Aesthetics.Data.AestheticsInterfaces.GHN;
using Aesthetics.Entities.Models.RequestModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aesthetics.Controllers.GHN
{
	[Route("api/[controller]")]
	[ApiController]
	public class GHNController : ControllerBase
	{
		private readonly IGHNService _ghnService;

		public GHNController(IGHNService ghnService)
		{
			_ghnService = ghnService;
		}

		[HttpGet("provinces")]
		public async Task<IActionResult> GetProvinces()
		{
			try
			{
				var result = await _ghnService.GetProvincesAsync();
				return Ok(result);
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = ex.Message });
			}
		}

		[HttpGet("districts/{provinceId}")]
		public async Task<IActionResult> GetDistricts(int provinceId)
		{
			try
			{
				var result = await _ghnService.GetDistrictsAsync(provinceId);
				return Ok(result);
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = ex.Message });
			}
		}

		[HttpGet("wards/{districtId}")]
		public async Task<IActionResult> GetWards(int districtId)
		{
			try
			{
				var result = await _ghnService.GetWardsAsync(districtId);
				return Ok(result);
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = ex.Message });
			}
		}

		[HttpPost("calculate-shipping-fee")]
		public async Task<IActionResult> CalculateShippingFee(CreateShippingOrderRequest createShippingOrder)
		{
			try
			{
				var result = await _ghnService.CalculateShippingFeesAsync(createShippingOrder, 2194, 6387655);
				return Ok(result);
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = ex.Message });
			}
		}

		[HttpPost("create-shipping-orders")]
		public async Task<IActionResult> CreateShippingOrders([FromBody] CreateShippingOrderRequest request)
		{
			try
			{
				if (request?.InvoiceIds == null || request.InvoiceIds.Count == 0)
				{
					return BadRequest(new { message = "Vui lòng cung cấp ít nhất một hóa đơn" });
				}

				var result = await _ghnService.CreateShippingOrdersAsync(request, 2194, 6387655);
				return Ok(result);
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = ex.Message });
			}
		}

		[HttpPost("return-shipping-orders")]
		public async Task<IActionResult> ReturnShippingOrders([FromBody] ReturnShippingOrderRequest request)
		{
			try
			{
				if (request?.InvoiceIds == null || request.InvoiceIds.Count == 0)
				{
					return BadRequest(new { message = "Vui lòng cung cấp ít nhất một ID hóa đơn để hoàn" });
				}

				var result = await _ghnService.ReturnShippingOrdersAsync(request, 6387655);
				return Ok(result);
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = ex.Message });
			}
		}

		//[HttpGet("available-services")]
		//public async Task<IActionResult> GetAvailableServices([FromQuery] int toCustomerId)
		//{
		//	try
		//	{
		//		var result = await _ghnService.GetAvailableServicesAsync(toCustomerId, 2194, 6387655);
		//		return Ok(result);
		//	}
		//	catch (Exception ex)
		//	{
		//		return StatusCode(500, new { message = ex.Message });
		//	}
		//}
	}
}
