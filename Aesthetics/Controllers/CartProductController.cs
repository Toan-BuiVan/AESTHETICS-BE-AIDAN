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
	public class CartProductController : ControllerBase
	{
		private readonly ICartProductService _cartProductService;

		public CartProductController(ICartProductService cartProductService)
		{
			_cartProductService = cartProductService;
		}

		[ServiceFilter(typeof(Filter_CheckToken))]
		[Filter_Authorization("createcartproduct")]
		[HttpPost("createcartproduct")]
		public async Task<IActionResult> Create([FromBody] CreateCartProduct request)
		{
			var result = await _cartProductService.create(request);
			return Ok(new { success = result });
		}

		[ServiceFilter(typeof(Filter_CheckToken))]
		[Filter_Authorization("updatecartproduct")]
		[HttpPost("updatecartproduct")]
		public async Task<IActionResult> Update([FromBody] UpdateCartProduct request)
		{
			var result = await _cartProductService.update(request);
			return Ok(new { success = result });
		}

		[ServiceFilter(typeof(Filter_CheckToken))]
		[Filter_Authorization("deletecartproduct")]
		[HttpPost("deletecartproduct")]
		public async Task<IActionResult> Delete([FromBody] DeleteCartProduct request)
		{
			var result = await _cartProductService.delete(request);
			return Ok(new { success = result });
		}

		//[ServiceFilter(typeof(Filter_CheckToken))]
		//[Filter_Authorization("getcartproductlist")]
		[HttpPost("getcartproductlist")]
		public async Task<IActionResult> GetList([FromBody] GetCartProduct request)
		{
			var result = await _cartProductService.getlist(request);
			return Ok(result);
		}
	}
}
