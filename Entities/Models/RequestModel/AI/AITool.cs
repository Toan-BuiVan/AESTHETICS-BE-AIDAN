using System;
using System.Collections.Generic;

namespace Aesthetics.Entities.Models.RequestModel.AI
{
	/// <summary>
	/// Định nghĩa một tool/API mà LLM có thể sử dụng
	/// </summary>
	public class AITool
	{
		/// <summary>Tên tool (unique)</summary>
		public string Name { get; set; }

		/// <summary>Mô tả chi tiết về tool này</summary>
		public string Description { get; set; }

		/// <summary>Danh sách tham số đầu vào và kiểu dữ liệu</summary>
		public Dictionary<string, string> InputSchema { get; set; }

		/// <summary>Mô tả output sẽ trả về</summary>
		public string OutputDescription { get; set; }

		/// <summary>Ví dụ sử dụng</summary>
		public string Example { get; set; }
	}

	/// <summary>
	/// Thông điệp trong lịch sử cuộc hội thoại
	/// </summary>
	public class AIConversationMessage
	{
		public string Role { get; set; } // "user" hoặc "assistant"
		public string Content { get; set; }
	}

	/// <summary>
	/// Response từ backend - danh sách các tool có sẵn
	/// </summary>
	public class AIToolsListResponse
	{
		/// <summary>System prompt cho LLM</summary>
		public string SystemPrompt { get; set; }

		/// <summary>Danh sách tất cả tools có sẵn</summary>
		public List<AITool> Tools { get; set; }

		/// <summary>Hướng dẫn format response</summary>
		public string ResponseFormat { get; set; }
	}

	/// <summary>
	/// Request từ frontend - yêu cầu từ người dùng
	/// </summary>
	public class AIFunctionCallRequest
	{
		/// <summary>Yêu cầu từ người dùng cuối</summary>
		public string UserQuery { get; set; }

		/// <summary>ID của user (để tracking và security)</summary>
		public int UserId { get; set; }

		/// <summary>Lịch sử cuộc hội thoại (nếu có)</summary>
		public List<AIConversationMessage> ConversationHistory { get; set; }
	}

	/// <summary>
	/// Response từ LLM - quyết định sử dụng tool nào
	/// </summary>
	public class AIFunctionCallResponse
	{
		/// <summary>Tên tool được chọn</summary>
		public string Tool { get; set; }

		/// <summary>Tham số để gọi API</summary>
		public Dictionary<string, object> Params { get; set; }

		/// <summary>Giải thích lý do chọn tool này (optional)</summary>
		public string Reasoning { get; set; }
	}

	/// <summary>
	/// Request để thực thi kết quả từ LLM
	/// </summary>
	public class AIExecuteToolRequest
	{
		/// <summary>Response từ LLM</summary>
		public AIFunctionCallResponse LLMResponse { get; set; }

		/// <summary>User ID để xác thực</summary>
		public int UserId { get; set; }
	}

	/// <summary>
	/// Response sau khi thực thi tool
	/// </summary>
	public class AIExecuteToolResponse
	{
		public bool Success { get; set; }
		public object Data { get; set; }
		public string Message { get; set; }
		public string Error { get; set; }
	}
}