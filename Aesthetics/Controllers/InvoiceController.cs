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

		//[HttpPost("updatepaymentstatus")]
		//public async Task<IActionResult> updatepaymentstatus([FromBody] UpdateInvoicePaymentStatus request)
		//{
		//	var result = await _invoiceService.UpdatePaymentStatus(request);
		//	return Ok(new { success = result });
		//}

		[HttpPost("updateinvoiceorderstatus")]
		public async Task<IActionResult> updateinvoiceorderstatus([FromBody] updateinvoiceorderstatus request)
		{
			var result = await _invoiceService.UpdateInvoiceOrderStatus(request);
			return Ok(new { success = result });
		}

		[HttpPost("getinvoicelist")]
		public async Task<IActionResult> GetList([FromBody] GetInvoice invoice)
		{
			var result = await _invoiceService.GetInvoiceDetails(invoice);
			return Ok(result);
		}

		/// <summary>
		/// 🆕 Update Status Invoice
		/// POST: /api/invoice/updatestatus
		/// </summary>
		[HttpPost("updatestatus")]
		public async Task<IActionResult> UpdateInvoiceStatus([FromBody] UpdateInvoiceStatusRequest request)
		{
			var result = await _invoiceService.UpdateInvoiceStatus(request.InvoiceId, request.NewStatus);

			if (!result)
			{
				return BadRequest(new { message = "Cập nhật status hóa đơn thất bại" });
			}

			return Ok(new
			{
				success = true,
				message = "Cập nhật status hóa đơn thành công",
				data = new { invoiceId = request.InvoiceId, newStatus = request.NewStatus }
			});
		}
	}
}
