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
	public class ProductController : ControllerBase
	{
		private readonly IProductService _productService;

		public ProductController(IProductService productService)
		{
			_productService = productService;
		}

		[HttpPost("createproduct")]
		public async Task<IActionResult> Create([FromBody] CreateProduct product)
		{
			var result = await _productService.create(product);
			return Ok(new { success = result });
		}

		[HttpPost("updateproduct")]
		public async Task<IActionResult> Update([FromBody] updateProduct product)
		{
			var result = await _productService.update(product);
			return Ok(new { success = result });
		}

		[HttpPost("deleteproduct")]
		public async Task<IActionResult> Delete([FromBody] deleteProduct product)
		{
			var result = await _productService.delete(product);
			return Ok(new { success = result });
		}

		[HttpPost("getproductlist")]
		public async Task<IActionResult> GetList([FromBody] getproduct product)
		{
			var result = await _productService.getlist(product);
			return Ok(result);
		}

		[HttpPost("exportproducttoexcel")]
		public async Task<IActionResult> ExportToExcel([FromBody] exportproduct product)
		{
			var result = await _productService.ExportToExcelAsync(product);
			return File(result, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Products.xlsx");
		}
	}
}
