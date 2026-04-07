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

		///// <summary>
		///// 🆕 Generate chatbot response khi không có tool hoặc không có kết quả
		///// Nếu là friendly question → trả lời ngay
		///// Nếu cần trí tuệ nhân tạo → gọi LLM
		///// </summary>
		//private async Task<dynamic> GenerateChatbotResponse(string userQuery, string errorMessage, bool useLLM = false)
		//{
		//	_logger.LogInformation("[CHATBOT_MODE] Generating response for: {Query}, UseLLM: {UseLLM}", userQuery, useLLM);

		//	// 🆕 Chatbot responses - nói chuyện thân thiện (không cần API)
		//	var friendlyResponses = new Dictionary<string, string>
		//	{
		//		// Lời chào
		//		{ "xin chào", "👋 Xin chào bạn! Mình là trợ lý AI của phòng khám thẩm mỹ. Mình có thể giúp bạn tìm kiếm dịch vụ, đặt lịch, hoặc trò chuyện cùng bạn. Bạn cần gì nào?" },
		//		{ "hi", "👋 Hi bạn! Rất vui được gặp bạn. Mình có thể hỗ trợ bạn về các dịch vụ thẩm mỹ, đặt lịch khám, hoặc bất cứ điều gì bạn cần!" },
		//		{ "hello", "👋 Hello! Welcome to our aesthetic clinic. How can I help you today?" },
		
		//		// Câu hỏi về mình
		//		{ "bạn là ai", "🤖 Mình là một trợ lý AI được thiết kế để hỗ trợ bạn tìm hiểu về các dịch vụ thẩm mỹ, đặt lịch khám, và trò chuyện về các vấn đề sắc đẹp." },
		//		{ "bạn tên gì", "👤 Mình là AI Assistant của phòng khám. Bạn có thể gọi mình là Bác sĩ AI hoặc chỉ gọi là AI!" },
		
		//		// Câu hỏi về khả năng
		//		{ "bạn có thể làm gì", "✨ Mình có thể giúp bạn:\n• 🏥 Tìm kiếm dịch vụ thẩm mỹ\n• 👨‍⚕️ Xem danh sách các bác sĩ\n• 📅 Đặt lịch khám\n• ❌ Hủy lịch khám\n• 💄 Tư vấn sản phẩm chăm sóc\n• 💬 Trò chuyện với bạn về sắc đẹp" },
		
		//		// Bộ lọc tuyệt vời
		//		{ "cảm ơn", "😊 Không có gì! Mình luôn sẵn lòng giúp bạn. Nếu có bất cứ câu hỏi nào khác, đừng ngần ngại hỏi mình nhé!" },
		//		{ "cảm ơn bạn", "🙌 Bạn thích rồi! Mình sẵn sàng giúp bạn bất cứ lúc nào." },
		//		{ "thanks", "😊 You're welcome! Feel free to ask me anything." },
		//	};

		//	var lowerQuery = userQuery.ToLower().Trim();

		//	// ✅ Check friendly questions TRƯỚC (không cần API)
		//	foreach (var kvp in friendlyResponses)
		//	{
		//		if (lowerQuery.Contains(kvp.Key))
		//		{
		//			// 🆕 Dùng dynamic object thay vì anonymous type
		//			dynamic response = new System.Dynamic.ExpandoObject();
		//			response.success = true;
		//			response.message = kvp.Value;
		//			response.data = null;
		//			response.toolUsed = "chatbot_friendly";
		//			response.conversationUpdate = new
		//			{
		//				role = "assistant",
		//				content = kvp.Value
		//			};
		//			return response;
		//		}
		//	}

		//	// 🆕 Nếu không phải friendly question và useLLM = true → gọi LLM để trả lời
		//	if (useLLM)
		//	{
		//		try
		//		{
		//			_logger.LogInformation("[CHATBOT_LLM] Calling LLM for intelligent response");

		//			var systemPrompt = @"Bạn là một trợ lý AI thân thiện của phòng khám thẩm mỹ.
		//				Trả lời câu hỏi của người dùng một cách tự nhiên, thân thiện, và hữu ích.
		//				Hãy giữ câu trả lời ngắn gọn (tối đa 200 từ).
		//				Nếu không biết câu trả lời, hãy nói 'Xin lỗi, tôi chưa hiểu. Bạn có thể nêu rõ hơn không?'";

		//			var llmResponse = await _llmService.CallLLMAsync(
		//				systemPrompt,
		//				userQuery,
		//				new List<LLMMessage>());

		//			if (!string.IsNullOrWhiteSpace(llmResponse))
		//			{
		//				// 🆕 Dùng dynamic object thay vì anonymous type
		//				dynamic response = new System.Dynamic.ExpandoObject();
		//				response.success = true;
		//				response.message = llmResponse;
		//				response.data = null;
		//				response.toolUsed = "chatbot_llm";
		//				response.conversationUpdate = new
		//				{
		//					role = "assistant",
		//					content = llmResponse
		//				};
		//				return response;
		//			}
		//		}
		//		catch (Exception ex)
		//		{
		//			_logger.LogError(ex, "[CHATBOT_LLM] Error calling LLM, falling back to default message");
		//		}
		//	}

		//	// ❌ Nếu không trùng khớp friendly response và không dùng LLM → trả lỗi
		//	// 🆕 Dùng dynamic object thay vì anonymous type
		//	dynamic finalResponse = new System.Dynamic.ExpandoObject();
		//	finalResponse.success = false;
		//	finalResponse.message = errorMessage;
		//	finalResponse.data = null;
		//	finalResponse.toolUsed = null;
		//	finalResponse.conversationUpdate = new
		//	{
		//		role = "assistant",
		//		content = errorMessage
		//	};
		//	return finalResponse;
		//}
		/// <summary>
		/// Lấy danh sách tất cả tools có sẵn
		/// </summary>
		//[HttpGet("get-available-tools")]
		//public async Task<IActionResult> GetAvailableTools()
		//{
		//	try
		//	{
		//		var tools = await _aiFunctionCallingService.GetAvailableToolsAsync();
		//		return Ok(new { success = true, data = tools });
		//	}
		//	catch (Exception ex)
		//	{
		//		_logger.LogError(ex, "Error getting available tools");
		//		return StatusCode(500, new { success = false, message = "Error getting tools", error = ex.Message });
		//	}
		//}
	}
}