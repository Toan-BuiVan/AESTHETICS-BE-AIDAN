using Aesthetics.Data.AestheticsInterfaces;
using Aesthetics.Data.AestheticsServices;
using Aesthetics.Entities.Entities;
using Aesthetics.Entities.Models.RequestModel;
using Aesthetics.Entities.Models.ResponseModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;

namespace Aesthetics.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class AccountController : ControllerBase
	{
		private readonly IAccountService _accountService;
		public AccountController(IAccountService accountService)
		{
			_accountService = accountService;
		}

		[HttpPost("createaccount")]
		public async Task<bool> create(RequestAccount account)
		{
			return await _accountService.create(account);
		}

		[HttpPost("updateaccount")]
		public async Task<bool> update(UpdateAccount account)
		{
			return await _accountService.update(account);
		}

		[HttpPost("deleteaccount")]
		public async Task<bool> detele(DeleteAccount account)
		{
			return await _accountService.delete(account);
		}

		[HttpPost("pagingaccount")]
		public async Task<BaseDataCollection<AccountEntity>> getlist(AccountGet account)
		{
			return await _accountService.getlist(account);
		}

		[HttpPost("getprofileaccount")]
		public async Task<AccountProfileResponseModel?> getprofile([FromBody] int accountId)
		{
			return await _accountService.GetProfileByAccountIdAsync(accountId);
		}
	}
}
