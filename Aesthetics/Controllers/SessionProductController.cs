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
	public class SessionProductController : ControllerBase
	{
		private readonly ISessionProductService _sessionProductService;

		public SessionProductController(ISessionProductService sessionProductService)
		{
			_sessionProductService = sessionProductService;
		}

		[HttpPost("createsessionproduct")]
		public async Task<IActionResult> Create([FromBody] CreateSessionProduct sessionProduct)
		{
			var result = await _sessionProductService.create(sessionProduct);
			return Ok(new { success = result });
		}

		[HttpPost("updatesessionproduct")]
		public async Task<IActionResult> Update([FromBody] UpdateSessionProduct sessionProduct)
		{
			var result = await _sessionProductService.update(sessionProduct);
			return Ok(new { success = result });
		}

		[HttpPost("deletesessionproduct")]
		public async Task<IActionResult> Delete([FromBody] DeleteSessionProduct sessionProduct)
		{
			var result = await _sessionProductService.delete(sessionProduct);
			return Ok(new { success = result });
		}

		[HttpPost("getsessionproductlist")]
		public async Task<IActionResult> GetList([FromBody] SessionProductGet sessionProduct)
		{
			var result = await _sessionProductService.getlist(sessionProduct);
			return Ok(result);
		}
	}
}
