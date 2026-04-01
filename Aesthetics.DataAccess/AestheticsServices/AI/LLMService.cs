using Aesthetics.Data.AestheticsInterfaces.AI;
using Aesthetics.Entities.Models.RequestModel.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Aesthetics.Data.AestheticsServices.AI
{
	public class LLMService : ILLMService
	{
		private readonly ILogger<LLMService> _logger;
		private readonly IConfiguration _configuration;
		private readonly HttpClient _httpClient;
		private readonly string _openAiApiKey;
		private readonly string _openAiBaseUrl = "https://api.openai.com/v1/chat/completions";

		public LLMService(
			ILogger<LLMService> logger,
			IConfiguration configuration,
			HttpClient httpClient)
		{
			_logger = logger;
			_configuration = configuration;
			_httpClient = httpClient;
			_openAiApiKey = configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI ApiKey not configured");
		}

		/// <summary>
		/// Gọi OpenAI API để xử lý yêu cầu
		/// </summary>
		public async Task<string> CallLLMAsync(string systemPrompt, string userMessage, List<LLMMessage> conversationHistory = null)
		{
			try
			{
				_logger.LogInformation("Calling LLM with user message: {UserMessage}", userMessage);

				// ✅ Xây dựng danh sách messages
				var messages = new List<LLMMessage>();

				// System prompt
				messages.Add(new LLMMessage
				{
					Role = "system",
					Content = systemPrompt
				});

				// Conversation history (nếu có)
				if (conversationHistory?.Any() == true)
				{
					messages.AddRange(conversationHistory);
				}

				// User message
				messages.Add(new LLMMessage
				{
					Role = "user",
					Content = userMessage
				});

				// ✅ Tạo request
				var request = new LLMRequest
				{
					Model = "gpt-4o-mini",
					Messages = messages,
					Temperature = 0,
					MaxTokens = 2000
				};

				var jsonContent = JsonSerializer.Serialize(request, new JsonSerializerOptions
				{
					PropertyNamingPolicy = JsonNamingPolicy.CamelCase
				});

				_logger.LogInformation("Sending request to OpenAI API with payload: {Payload}", jsonContent);

				var httpRequest = new HttpRequestMessage(HttpMethod.Post, _openAiBaseUrl)
				{
					Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
				};

				// ✅ Thêm API key vào header
				httpRequest.Headers.Add("Authorization", $"Bearer {_openAiApiKey}");

				// ✅ Gọi API
				var response = await _httpClient.SendAsync(httpRequest);

				if (!response.IsSuccessStatusCode)
				{
					var errorContent = await response.Content.ReadAsStringAsync();
					_logger.LogError("OpenAI API error: {StatusCode} - {ErrorContent}",
						response.StatusCode, errorContent);
					throw new Exception($"OpenAI API error: {response.StatusCode} - {errorContent}");
				}

				// ✅ Parse response
				var responseContent = await response.Content.ReadAsStringAsync();
				_logger.LogInformation("OpenAI API response: {Response}", responseContent);

				var llmResponse = JsonSerializer.Deserialize<LLMResponse>(responseContent, new JsonSerializerOptions
				{
					PropertyNameCaseInsensitive = true
				});

				if (llmResponse?.Choices == null || !llmResponse.Choices.Any())
				{
					throw new Exception("Invalid response from OpenAI API - no choices found");
				}

				var assistantMessage = llmResponse.Choices[0].Message.Content;

				_logger.LogInformation("LLM response: {Response}", assistantMessage);
				_logger.LogInformation("Token usage - Prompt: {PromptTokens}, Completion: {CompletionTokens}, Total: {TotalTokens}",
					llmResponse.Usage.PromptTokens, llmResponse.Usage.CompletionTokens, llmResponse.Usage.TotalTokens);

				return assistantMessage;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error calling LLM: {ErrorMessage}", ex.Message);
				throw;
			}
		}

		/// <summary>
		/// Parse LLM response thành AIFunctionCallResponse
		/// Ví dụ: LLM trả về JSON như:
		/// {
		///   "tool": "getServiceAvailableSlots",
		///   "params": { "serviceId": 1, "startDate": "2026-03-31", "endDate": "2026-04-06" }
		/// }
		/// </summary>
		public AIFunctionCallResponse ParseLLMResponse(string llmResponseText)
		{
			try
			{
				_logger.LogInformation("Parsing LLM response");

				// ✅ Tìm JSON block trong response (nằm giữa { và })
				var jsonStartIndex = llmResponseText.IndexOf('{');
				var jsonEndIndex = llmResponseText.LastIndexOf('}');

				if (jsonStartIndex < 0 || jsonEndIndex < 0)
				{
					_logger.LogWarning("No JSON found in LLM response: {Response}", llmResponseText);
					throw new Exception("Invalid LLM response format - no JSON found");
				}

				var jsonString = llmResponseText.Substring(jsonStartIndex, jsonEndIndex - jsonStartIndex + 1);

				_logger.LogInformation("Extracted JSON: {Json}", jsonString);

				// ✅ Parse JSON
				var options = new JsonSerializerOptions
				{
					PropertyNameCaseInsensitive = true
				};

				var result = JsonSerializer.Deserialize<AIFunctionCallResponse>(jsonString, options);

				if (result == null || string.IsNullOrEmpty(result.Tool))
				{
					throw new Exception("Failed to parse LLM response into AIFunctionCallResponse");
				}

				_logger.LogInformation("Parsed tool: {Tool}, params count: {ParamCount}",
					result.Tool, result.Params?.Count ?? 0);

				return result;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error parsing LLM response: {Response}", llmResponseText);
				throw;
			}
		}
	}
}