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
	public class InvoiceController : ControllerBase
	{
		private readonly IInvoiceService _invoiceService;

		public InvoiceController(IInvoiceService invoiceService)
		{
			_invoiceService = invoiceService;
		}

		[ServiceFilter(typeof(Filter_CheckToken))]
		[Filter_Authorization("createinvoice")]
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

		[ServiceFilter(typeof(Filter_CheckToken))]
		[Filter_Authorization("updateinvoiceorderstatus")]
		[HttpPost("updateinvoiceorderstatus")]
		public async Task<IActionResult> updateinvoiceorderstatus([FromBody] updateinvoiceorderstatus request)
		{
			var result = await _invoiceService.UpdateInvoiceOrderStatus(request);
			return Ok(new { success = result });
		}

		//[ServiceFilter(typeof(Filter_CheckToken))]
		//[Filter_Authorization("getinvoicelist")]
		[HttpPost("getinvoicelist")]
		public async Task<IActionResult> GetList([FromBody] GetInvoice invoice)
		{
			var result = await _invoiceService.GetInvoiceDetails(invoice);
			return Ok(result);
		}


		[ServiceFilter(typeof(Filter_CheckToken))]
		[Filter_Authorization("updatestatus")]
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

		
		[HttpPost("export")]
		[Produces("application/json")]
		public async Task<IActionResult> ExportInvoices([FromBody] ExportInvoiceOrder exportInvoice)
		{
			try
			{
				var exportedInvoices = await _invoiceService.ExportInvoicesByIdListAsync(exportInvoice);

				if (exportedInvoices == null || exportedInvoices.Count == 0)
				{
					return NotFound(new { message = "Không tìm thấy hóa đơn nào để xuất" });
				}

				return Ok(new
				{
					success = true,
					totalCount = exportedInvoices.Count,
					data = exportedInvoices
				});
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = "Lỗi khi xuất hóa đơn", error = ex.Message });
			}
		}
	}
}
