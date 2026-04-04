using Aesthetics.Data.AestheticsInterfaces;
using Aesthetics.Data.AestheticsInterfaces.AI;
using Aesthetics.Entities.Models.RequestModel.AI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Aesthetics.Controllers.AI
{
	[Route("api/[controller]")]
	[ApiController]
	public class AIFunctionCallingController : ControllerBase
	{
		private readonly IAIFunctionCallingService _aiFunctionCallingService;
		private readonly ILLMService _llmService;
		private readonly ILogger<AIFunctionCallingController> _logger;

		public AIFunctionCallingController(
			IAIFunctionCallingService aiFunctionCallingService,
			ILLMService llmService,
			ILogger<AIFunctionCallingController> logger)
		{
			_aiFunctionCallingService = aiFunctionCallingService;
			_llmService = llmService;
			_logger = logger;
		}

		/// <summary>
		/// 🔥 MAIN ENDPOINT: Xử lý query từ người dùng
		/// </summary>
		[HttpPost("process-user-query")]
		public async Task<IActionResult> ProcessUserQuery([FromBody] AIFunctionCallRequest request)
		{
			try
			{
				// ✅ VALIDATE REQUEST
				if (request == null)
				{
					return BadRequest(new { success = false, message = "Request cannot be null" });
				}

				if (string.IsNullOrWhiteSpace(request.UserQuery))
				{
					return BadRequest(new { success = false, message = "UserQuery is required" });
				}

				if (request.UserId <= 0)
				{
					return BadRequest(new { success = false, message = "UserId must be greater than 0" });
				}

				_logger.LogInformation(
					"AI Query received - UserId: {UserId}, Query: {Query}",
					request.UserId,
					request.UserQuery);

				// ✅ GỌI SERVICE - XỬ LÝ
				var response = await _aiFunctionCallingService.ProcessUserQueryAsync(request);

				return Ok(response);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error processing user query");
				return StatusCode(500, new
				{
					success = false,
					message = "Internal server error",
					error = ex.Message
				});
			}
		}

		/// <summary>
		/// Lấy danh sách tất cả tools có sẵn
		/// </summary>
		[HttpGet("get-available-tools")]
		public async Task<IActionResult> GetAvailableTools()
		{
			try
			{
				var tools = await _aiFunctionCallingService.GetAvailableToolsAsync();
				return Ok(new { success = true, data = tools });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting available tools");
				return StatusCode(500, new { success = false, message = "Error getting tools", error = ex.Message });
			}
		}
	}
}