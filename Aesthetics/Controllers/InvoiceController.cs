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
	public class InvoiceController : ControllerBase
	{
		private readonly IInvoiceService _invoiceService;

		public InvoiceController(IInvoiceService invoiceService)
		{
			_invoiceService = invoiceService;
		}

		[HttpPost("createinvoice")]
		public async Task<IActionResult> Create([FromBody] CreateInvoice invoice)
		{
			var result = await _invoiceService.create(invoice);
			return Ok(new { success = result });
		}

		[HttpPost("getinvoicelist")]
		public async Task<IActionResult> GetList([FromBody] GetInvoice invoice)
		{
			var result = await _invoiceService.getlist(invoice);
			return Ok(result);
		}
	}
}
