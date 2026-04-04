using Aesthetics.Entities.Models.RequestModel;
using Aesthetics.Entities.Models.RequestModel.AI;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aesthetics.Data.AestheticsInterfaces
{
	public interface IAIFunctionCallingService
	{
		/// <summary>
		/// 🔥 Main method: Xử lý query từ người dùng
		/// Luồng: Query → LLM → Tool → Result → Response
		/// </summary>
		Task<dynamic> ProcessUserQueryAsync(AIFunctionCallRequest request);

		/// <summary>
		/// Lấy danh sách tất cả tools (APIs) có sẵn cho LLM
		/// </summary>
		Task<AIToolsListResponse> GetAvailableToolsAsync();

		/// <summary>
		/// Thực thi tool được LLM chọn
		/// </summary>
		Task<AIExecuteToolResponse> ExecuteToolAsync(AIExecuteToolRequest request);

		/// <summary>
		/// Validate các tham số trước khi thực thi
		/// </summary>
		Task<bool> ValidateToolParamsAsync(string toolName, Dictionary<string, object> @params);
	}
}