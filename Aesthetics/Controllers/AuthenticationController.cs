using Aesthetics.Data.AestheticsInterfaces;
using Aesthetics.Entities.Models.RequestModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aesthetics.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
		private IAuthenticationService _authenticationService;
		public AuthenticationController(IAuthenticationService authenticationService)
		{
			_authenticationService = authenticationService;
		}

		[HttpPost("login")]
		public async Task<IActionResult> Login([FromBody] RequestLogin request)
		{
			var result = await _authenticationService.login(request);
			return Ok(result);
		}

		[HttpPost("logout")]
		public async Task<IActionResult> Logout([FromBody] RequestLogout request)
		{
			var result = await _authenticationService.logout(request);
			return Ok("Logout success");
		}

		[HttpPost("logoutall")]
		public async Task<IActionResult> LogoutAll([FromBody] RequestLogout request)
		{
			var result = await _authenticationService.logoutall(request);
			return Ok("Logout all success");
		}
	}
}
