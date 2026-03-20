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
	public class VoucherController : ControllerBase
	{
		private readonly IVoucherService _voucherService;

		public VoucherController(IVoucherService voucherService)
		{
			_voucherService = voucherService;
		}

		[HttpPost("createvoucher")]
		public async Task<IActionResult> Create([FromBody] CreateVoucher voucher)
		{
			var result = await _voucherService.create(voucher);
			return Ok(new { success = result });
		}

		[HttpPost("updatevoucher")]
		public async Task<IActionResult> Update([FromBody] UpdateVoucher voucher)
		{
			var result = await _voucherService.update(voucher);
			return Ok(new { success = result });
		}

		[HttpPost("deletevoucher")]
		public async Task<IActionResult> Delete([FromBody] DeleteClinic voucher)
		{
			var result = await _voucherService.delete(voucher);
			return Ok(new { success = result });
		}

		[HttpPost("getvoucherlist")]
		public async Task<IActionResult> GetList([FromBody] VoucherGet voucher)
		{
			var result = await _voucherService.getlist(voucher);
			return Ok(result);
		}
	}
}
