using Aesthetics.Data.AestheticsInterfaces;
using Aesthetics.Data.AestheticsInterfaces.AI;
using Aesthetics.Entities.Models.RequestModel;
using Aesthetics.Entities.Models.RequestModel.AI;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Aesthetics.Controllers.AI
{
	[Route("api/[controller]")]
	[ApiController]
	public class AIFunctionCallingController : ControllerBase
	{
		private readonly IAIFunctionCallingService _aiFunctionCallingService;
		private readonly ILLMService _llmService;

		public AIFunctionCallingController(
			IAIFunctionCallingService aiFunctionCallingService,
			ILLMService llmService)
		{
			_aiFunctionCallingService = aiFunctionCallingService;
			_llmService = llmService;
		}

		/// <summary>
		/// Lấy danh sách tất cả tools (APIs) có sẵn cho LLM
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
				return StatusCode(500, new { success = false, message = "Error getting available tools", error = ex.Message });
			}
		}

		/// <summary>
		/// Endpoint tích hợp hoàn chỉnh: Nhận query từ người dùng → Gọi LLM → Thực thi tool → Trả kết quả
		/// </summary>
		/// <param name="request">Query từ người dùng</param>
		/// <returns>Kết quả thực thi tool</returns>
		[HttpPost("process-user-query")]
		public async Task<IActionResult> ProcessUserQuery([FromBody] AIFunctionCallRequest request)
		{
			try
			{
				if (request == null || string.IsNullOrEmpty(request.UserQuery))
				{
					return BadRequest(new { success = false, message = "UserQuery is required" });
				}

				if (request.UserId <= 0)
				{
					return BadRequest(new { success = false, message = "UserId is required and must be greater than 0" });
				}

				// ✅ Bước 1: Lấy danh sách tools
				var toolsList = await _aiFunctionCallingService.GetAvailableToolsAsync();

				// ✅ Bước 2: Tạo system prompt + tools definition
				var systemPrompt = BuildSystemPrompt(toolsList);

				// ✅ Bước 3: Chuyển đổi AIConversationMessage thành LLMMessage
				List<LLMMessage> llmConversationHistory = null;
				if (request.ConversationHistory?.Any() == true)
				{
					llmConversationHistory = request.ConversationHistory
						.Select(m => new LLMMessage
						{
							Role = m.Role,
							Content = m.Content
						})
						.ToList();
				}

				// ✅ Bước 4: Gọi LLM để quyết định sử dụng tool nào
				var llmResponse = await _llmService.CallLLMAsync(
					systemPrompt,
					request.UserQuery,
					llmConversationHistory);

				// ✅ Bước 5: Parse LLM response
				AIFunctionCallResponse functionCall;
				try
				{
					functionCall = _llmService.ParseLLMResponse(llmResponse);
				}
				catch (Exception ex)
				{
					return BadRequest(new
					{
						success = false,
						message = "Failed to parse LLM response",
						error = ex.Message,
						llmResponse
					});
				}

				// ✅ Bước 6: Thực thi tool
				var executeRequest = new AIExecuteToolRequest
				{
					LLMResponse = functionCall,
					UserId = request.UserId
				};

				var toolResult = await _aiFunctionCallingService.ExecuteToolAsync(executeRequest);

				if (!toolResult.Success)
				{
					return BadRequest(new
					{
						success = false,
						message = toolResult.Message,
						error = toolResult.Error
					});
				}

				// ✅ Bước 7: Trả kết quả
				return Ok(new
				{
					success = true,
					userQuery = request.UserQuery,
					tool = functionCall.Tool,
					toolParams = functionCall.Params,
					reasoning = functionCall.Reasoning,
					toolResult = new
					{
						success = toolResult.Success,
						data = toolResult.Data,
						message = toolResult.Message
					}
				});
			}
			catch (Exception ex)
			{
				return StatusCode(500, new
				{
					success = false,
					message = "Error processing user query",
					error = ex.Message
				});
			}
		}

		/// <summary>
		/// Thực thi tool được chỉ định (dùng khi đã có LLM response)
		/// </summary>
		[HttpPost("execute-tool")]
		public async Task<IActionResult> ExecuteTool([FromBody] AIExecuteToolRequest request)
		{
			try
			{
				if (request?.LLMResponse == null)
				{
					return BadRequest(new { success = false, message = "LLMResponse is required" });
				}

				if (request.UserId <= 0)
				{
					return BadRequest(new { success = false, message = "UserId is required and must be greater than 0" });
				}

				var result = await _aiFunctionCallingService.ExecuteToolAsync(request);
				return Ok(new { success = result.Success, data = result.Data, message = result.Message, error = result.Error });
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { success = false, message = "Error executing tool", error = ex.Message });
			}
		}

		/// <summary>
		/// Xây dựng system prompt chứa hướng dẫn và danh sách tools
		/// </summary>
		private string BuildSystemPrompt(AIToolsListResponse toolsList)
		{
			var toolsJson = System.Text.Json.JsonSerializer.Serialize(toolsList.Tools, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

			return $@"{toolsList.SystemPrompt}

				DANH SÁCH CÁC TOOL CÓ SẴN:
				{toolsJson}

				FORMAT RESPONSE:
				{toolsList.ResponseFormat}

				HƯỚNG DẪN:
				- Phân tích kỹ yêu cầu của người dùng
				- Chọn tool phù hợp nhất
				- Điền đầy đủ tất cả tham số bắt buộc
				- Trả về JSON hợp lệ
				- Nếu không chắc, hãy chọn tool có liên quan nhất hoặc hỏi làm rõ";
		}
	}
}