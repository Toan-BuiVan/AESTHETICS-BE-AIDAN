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
	public class InvoiceDetailController : ControllerBase
	{
		private readonly IInvoiceDetailService _invoiceDetailService;

		public InvoiceDetailController(IInvoiceDetailService invoiceDetailService)
		{
			_invoiceDetailService = invoiceDetailService;
		}

		//[ServiceFilter(typeof(Filter_CheckToken))]
		//[Filter_Authorization("getinvoicedetaillist")]
		[HttpGet("getinvoicedetaillist/{invoiceId}")]
		public async Task<IActionResult> GetList(int invoiceId)
		{
			var result = await _invoiceDetailService.getlist(invoiceId);
			return Ok(result);
		}
	}
}
