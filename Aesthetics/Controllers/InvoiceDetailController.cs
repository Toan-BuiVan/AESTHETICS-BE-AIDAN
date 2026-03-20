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
	public class InvoiceDetailController : ControllerBase
	{
		private readonly IInvoiceDetailService _invoiceDetailService;

		public InvoiceDetailController(IInvoiceDetailService invoiceDetailService)
		{
			_invoiceDetailService = invoiceDetailService;
		}

		[HttpGet("getinvoicedetaillist/{invoiceId}")]
		public async Task<IActionResult> GetList(int invoiceId)
		{
			var result = await _invoiceDetailService.getlist(invoiceId);
			return Ok(result);
		}
	}
}
