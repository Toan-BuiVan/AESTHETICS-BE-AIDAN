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
	public class WalletController : ControllerBase
	{
		private readonly IWalletService _walletService;

		public WalletController(IWalletService walletService)
		{
			_walletService = walletService;
		}

		[ServiceFilter(typeof(Filter_CheckToken))]
		[Filter_Authorization("createwallet")]
		[HttpPost("createwallet")]
		public async Task<IActionResult> Create([FromBody] CreateWallet wallet)
		{
			var result = await _walletService.create(wallet);
			return Ok(new { success = result });
		}

		//[HttpPost("deletewallet")]
		//public async Task<IActionResult> Delete([FromBody] DeleteWallest wallet)
		//{
		//	var result = await _walletService.delete(wallet);
		//	return Ok(new { success = result });
		//}

		//[ServiceFilter(typeof(Filter_CheckToken))]
		//[Filter_Authorization("getwalletlist")]
		[HttpPost("getwalletlist")]
		public async Task<IActionResult> GetList([FromBody] WalletGet wallet)
		{
			var result = await _walletService.getlist(wallet);
			return Ok(result);
		}

		[ServiceFilter(typeof(Filter_CheckToken))]
		[Filter_Authorization("exchangevoucher")]
		[HttpPost("exchangevoucher")]
		public async Task<bool> ExchangeVoucher([FromBody] RequestExchangeVoucher request)
		{
			return await _walletService.ExchangeVoucherAsync(request);
		}
	}
}
