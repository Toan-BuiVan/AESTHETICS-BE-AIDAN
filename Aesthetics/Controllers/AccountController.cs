using Aesthetics.Data.AestheticsInterfaces;
using Aesthetics.Data.AestheticsServices;
using Aesthetics.Entities.Entities;
using Aesthetics.Entities.Models.RequestModel;
using Aesthetics.Entities.Models.ResponseModel;
using ASP_NetCore_Aesthetics.Filter;
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
		private readonly IAccountSessionsService _accountSessionsService;
		public AccountController(IAccountService accountService, IAccountSessionsService accountSessionsService)
		{
			_accountService = accountService;
			_accountSessionsService = accountSessionsService;
		}

		[HttpPost("createaccount")]
		public async Task<bool> create(RequestAccount account)
		{
			return await _accountService.create(account);
		}

		[ServiceFilter(typeof(Filter_CheckToken))]
		[Filter_Authorization("updateclinic")]
		[HttpPost("updateaccount")]
		public async Task<bool> update(UpdateAccount account)
		{
			return await _accountService.update(account);
		}

		[ServiceFilter(typeof(Filter_CheckToken))]
		[Filter_Authorization("deleteaccount")]
		[HttpPost("deleteaccount")]
		public async Task<bool> detele(DeleteAccount account)
		{
			return await _accountService.delete(account);
		}

		//[ServiceFilter(typeof(Filter_CheckToken))]
		//[Filter_Authorization("pagingaccount")]
		[HttpPost("pagingaccount")]
		public async Task<BaseDataCollection<AccountEntity>> getlist(AccountGet account)
		{
			return await _accountService.getlist(account);
		}

		//[ServiceFilter(typeof(Filter_CheckToken))]
		//[Filter_Authorization("getprofileaccount")]
		[HttpPost("getprofileaccount")]
		public async Task<AccountProfileResponseModel?> getprofile(int accountId)
		{
			return await _accountService.GetProfileByAccountIdAsync(accountId);
		}

		//[ServiceFilter(typeof(Filter_CheckToken))]
		//[Filter_Authorization("getaccountsession")]
		[HttpPost("getaccountsession")]
		public async Task<BaseDataCollection<AccountSessionEntity?>> getaccountsession(getSession session)
		{
			return await _accountSessionsService.getlist(session);
		}
	}
}
