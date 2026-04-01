using Aesthetics.Entities.Models.RequestModel.AI;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aesthetics.Data.AestheticsInterfaces.AI
{
	public interface ILLMService
	{
		/// <summary>
		/// Gọi LLM (OpenAI) để phân tích yêu cầu của người dùng
		/// và quyết định sử dụng tool nào
		/// </summary>
		/// <param name="systemPrompt">System prompt hướng dẫn LLM</param>
		/// <param name="userMessage">Yêu cầu từ người dùng</param>
		/// <param name="conversationHistory">Lịch sử cuộc hội thoại (optional)</param>
		/// <returns>Response từ LLM</returns>
		Task<string> CallLLMAsync(string systemPrompt, string userMessage, List<LLMMessage> conversationHistory = null);

		/// <summary>
		/// Parse LLM response thành AIFunctionCallResponse
		/// </summary>
		AIFunctionCallResponse ParseLLMResponse(string llmResponseText);
	}
}