using Aesthetics.Data.RepositoryInterfaces;
using Aesthetics.Data.AestheticsInterfaces.TokenService;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ASP_NetCore_Aesthetics.Filter
{
	public class Filter_Authorization : TypeFilterAttribute
	{
		public Filter_Authorization(string functionCode) : base(typeof(AuthorizeActionFileter))
		{
			Arguments = new object[] { functionCode };
		}
	}

	public class AuthorizeActionFileter : IAsyncAuthorizationFilter
	{
		private readonly string _functionCode;
		private readonly IAccountRepository _accountRepository;
		private readonly ITokenService _tokenService;

		public AuthorizeActionFileter(string functionCode,
			IAccountRepository accountRepository,
			ITokenService tokenService)
		{
			_accountRepository = accountRepository;
			_tokenService = tokenService;
			_functionCode = functionCode;
		}

		public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
		{
			// Lấy token từ header
			var authHeader = context.HttpContext.Request.Headers["Authorization"].FirstOrDefault();
			if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
			{
				SetUnauthorizedResponse(context, "Vui lòng đăng nhập để thực hiện chức năng này");
				return;
			}

			var token = authHeader.Replace("Bearer ", "");

			try
			{
				// Decode JWT token để lấy claims
				var handler = new JwtSecurityTokenHandler();
				var jwtToken = handler.ReadJwtToken(token);
				var userId = jwtToken.Claims.FirstOrDefault(x => x.Type == ClaimTypes.PrimarySid)?.Value;

				if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt) || userIdInt == 0)
				{
					SetUnauthorizedResponse(context, "Vui lòng đăng nhập để thực hiện chức năng này");
					return;
				}

				// Lấy FunctionID dựa theo FunctionCode
				var function = await _accountRepository.GetFunctionIDByName(_functionCode);
				if (function == null)
				{
					SetUnauthorizedResponse(context, "Chức năng này không hợp lệ");
					return;
				}

				// Kiểm tra quyền
				var permission = await _accountRepository.GetPermisstionUserIDOfFunctionID(userIdInt, function.Id);
				if (permission == null || permission.IsActive == false)
				{
					SetUnauthorizedResponse(context, "Bạn không có quyền thực hiện chức năng này");
					return;
				}
			}
			catch (Exception ex)
			{
				SetUnauthorizedResponse(context, "Token không hợp lệ");
				return;
			}
		}

		private void SetUnauthorizedResponse(AuthorizationFilterContext context, string message)
		{
			context.HttpContext.Response.ContentType = "application/json";
			context.HttpContext.Response.StatusCode = (int)System.Net.HttpStatusCode.Unauthorized;
			context.Result = new JsonResult(new
			{
				ReturnCode = System.Net.HttpStatusCode.Unauthorized,
				ReturnMessage = message
			});
		}
	}
}