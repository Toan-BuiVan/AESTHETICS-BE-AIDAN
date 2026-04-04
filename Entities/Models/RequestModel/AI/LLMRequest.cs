using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Aesthetics.Entities.Models.RequestModel.AI
{
	/// <summary>
	/// Request gửi tới LLM (OpenAI)
	/// </summary>
	public class LLMRequest
	{
		[JsonPropertyName("model")]
		public string Model { get; set; } = "gpt-4o-mini";

		[JsonPropertyName("messages")]
		public List<LLMMessage> Messages { get; set; } = new();

		[JsonPropertyName("temperature")]
		public int Temperature { get; set; } = 0;

		[JsonPropertyName("max_tokens")]
		public int MaxTokens { get; set; } = 2000;
	}

	/// <summary>
	/// Message trong conversation với LLM
	/// </summary>
	public class LLMMessage
	{
		public string Role { get; set; } // "system", "user", "assistant"
		public string Content { get; set; }
	}

	public class LLMToolResponse
	{
		public string Tool { get; set; }
		public System.Collections.Generic.Dictionary<string, object> Params { get; set; }
	}

	/// <summary>
	/// Response từ LLM (OpenAI)
	/// </summary>
	public class LLMResponse
	{
		public string Id { get; set; }
		public string Object { get; set; }
		public long Created { get; set; }
		public string Model { get; set; }
		public List<LLMChoice> Choices { get; set; }
		public LLMUsage Usage { get; set; }
	}

	/// <summary>
	/// Choice từ LLM response
	/// </summary>
	public class LLMChoice
	{
		public int Index { get; set; }
		public LLMMessage Message { get; set; }
		public string FinishReason { get; set; }
	}

	/// <summary>
	/// Token usage statistics
	/// </summary>
	public class LLMUsage
	{
		public int PromptTokens { get; set; }
		public int CompletionTokens { get; set; }
		public int TotalTokens { get; set; }
	}
}