using Aesthetics.Data.AestheticsInterfaces;
using Aesthetics.Data.AestheticsInterfaces.AI;
using Aesthetics.Data.RepositoryInterfaces;
using Aesthetics.Entities.Entities;
using Aesthetics.Entities.Models.RequestModel;
using Aesthetics.Entities.Models.RequestModel.AI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
		private readonly IStaffRepository _staffRepository;
		private readonly IServiceRepository _serviceRepository;
		private readonly IProductRepository _productRepository;
		private readonly ILogger<AIFunctionCallingController> _logger;

		public AIFunctionCallingController(
			IAIFunctionCallingService aiFunctionCallingService,
			ILLMService llmService,
			IStaffRepository staffRepository,
			IServiceRepository serviceRepository,
			IProductRepository productRepository,
			ILogger<AIFunctionCallingController> logger)
		{
			_aiFunctionCallingService = aiFunctionCallingService;
			_llmService = llmService;
			_staffRepository = staffRepository;
			_serviceRepository = serviceRepository;
			_productRepository = productRepository;
			_logger = logger;
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
		/// Endpoint tích hợp hoàn chỉnh: Nhận query từ người dùng → Enrich query → Gọi LLM → Thực thi tool → Trả kết quả
		/// </summary>
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

				// ✅ PREPROCESSING: Enrich query bằng cách tìm doctor/service/product
				var enrichedQuery = await EnrichUserQueryAsync(request.UserQuery);
				
				_logger.LogInformation("Original query: {OriginalQuery}", request.UserQuery);
				_logger.LogInformation("Enriched query: {EnrichedQuery}", enrichedQuery);

				// ✅ Bước 1: Lấy danh sách tools
				var toolsList = await _aiFunctionCallingService.GetAvailableToolsAsync();

				// ✅ Bước 2: Tạo system prompt + tools definition
				var systemPrompt = BuildSystemPrompt(toolsList);

				// ✅ Bước 3: Chuyển đổi conversation history
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

				// ✅ Bước 4: Gọi LLM với enriched query
				var llmResponse = await _llmService.CallLLMAsync(
					systemPrompt,
					enrichedQuery,
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
					enrichedQuery = enrichedQuery,
					tool = functionCall.Tool,
					toolParameters = functionCall.Params,
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
		/// ✅ Preprocessing: Enrich user query bằng cách tìm ID cho doctor/service/product và normalize date
		/// </summary>
		private async Task<string> EnrichUserQueryAsync(string userQuery)
		{
			var enrichedQuery = userQuery;

			try
			{
				// ✅ Normalize & enrich dates (DD-MM-YYYY → YYYY-MM-DD)
				enrichedQuery = NormalizeDates(enrichedQuery);
				
				_logger.LogInformation("After date normalization: {Query}", enrichedQuery);

				// ✅ Tìm doctor từ tên
				var doctor = await FindDoctorByNameAsync(enrichedQuery);
				if (doctor != null)
				{
					enrichedQuery = ReplaceWithTag(enrichedQuery, doctor.FullName, 
						$"[Doctor: ID={doctor.Id}, Name={doctor.FullName}]");
					_logger.LogInformation("Enriched with doctor: {Doctor} (ID={Id})", doctor.FullName, doctor.Id);
				}

				// ✅ Tìm service từ tên
				var service = await FindServiceByNameAsync(enrichedQuery);
				if (service != null)
				{
					enrichedQuery = ReplaceWithTag(enrichedQuery, service.ServiceName,
						$"[Service: ID={service.Id}, Name={service.ServiceName}]");
					_logger.LogInformation("Enriched with service: {Service} (ID={Id})", service.ServiceName, service.Id);
				}

				// ✅ Tìm product từ tên, loại, hoặc nhà cung cấp
				var product = await FindProductAsync(enrichedQuery);
				if (product != null)
				{
					enrichedQuery = ReplaceWithTag(enrichedQuery, product.ProductName,
						$"[Product: ID={product.Id}, Name={product.ProductName}]");
					_logger.LogInformation("Enriched with product: {Product} (ID={Id})", product.ProductName, product.Id);
				}

				return enrichedQuery;
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Error enriching query, returning original query");
				return userQuery;
			}
		}

		/// <summary>
		/// Normalize dates từ DD-MM-YYYY hoặc DD/MM/YYYY thành YYYY-MM-DD
		/// VD: "04-04-2026" → "2026-04-04"
		/// </summary>
		private string NormalizeDates(string query)
		{
			try
			{
				// Pattern 1: DD-MM-YYYY
				var pattern1 = @"(\d{1,2})-(\d{1,2})-(\d{4})";
				var normalized1 = System.Text.RegularExpressions.Regex.Replace(query, pattern1, (match) =>
				{
					var day = match.Groups[1].Value.PadLeft(2, '0');
					var month = match.Groups[2].Value.PadLeft(2, '0');
					var year = match.Groups[3].Value;
					
					// Validate date
					if (int.TryParse(day, out var d) && int.TryParse(month, out var m) && 
						d >= 1 && d <= 31 && m >= 1 && m <= 12)
					{
						return $"{year}-{month}-{day}";
					}
					return match.Value;
				});

				// Pattern 2: DD/MM/YYYY
				var pattern2 = @"(\d{1,2})/(\d{1,2})/(\d{4})";
				var normalized2 = System.Text.RegularExpressions.Regex.Replace(normalized1, pattern2, (match) =>
				{
					var day = match.Groups[1].Value.PadLeft(2, '0');
					var month = match.Groups[2].Value.PadLeft(2, '0');
					var year = match.Groups[3].Value;
					
					// Validate date
					if (int.TryParse(day, out var d) && int.TryParse(month, out var m) && 
						d >= 1 && d <= 31 && m >= 1 && m <= 12)
					{
						return $"{year}-{month}-{day}";
					}
					return match.Value;
				});

				return normalized2;
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Error normalizing dates");
				return query;
			}
		}

		/// <summary>
		/// Helper để replace text case-insensitive
		/// </summary>
		private string ReplaceWithTag(string text, string oldValue, string newValue)
		{
			if (string.IsNullOrEmpty(oldValue))
				return text;

			return System.Text.RegularExpressions.Regex.Replace(
				text,
				System.Text.RegularExpressions.Regex.Escape(oldValue),
				newValue,
				System.Text.RegularExpressions.RegexOptions.IgnoreCase
			);
		}

		/// <summary>
		/// Tìm bác sĩ theo tên (Fuzzy matching)
		/// VD: "Toan" khớp "Bui Van Toan"
		/// </summary>
		private async Task<StaffEntity> FindDoctorByNameAsync(string query)
		{
			try
			{
				var doctors = await _staffRepository.FindByPredicate(d => d.IsDoctor == true);
				
				var queryLower = query.ToLower();
				var doctor = doctors.FirstOrDefault(d =>
				{
					var nameLower = d.FullName.ToLower();
					
					// Cách 1: Tên chứa query
					if (nameLower.Contains(queryLower))
						return true;
					
					// Cách 2: Bất kỳ từ nào từ query khớp với từ từ tên
					var nameWords = nameLower.Split(new[] { " ", "-" }, StringSplitOptions.RemoveEmptyEntries);
					var queryWords = queryLower.Split(new[] { " ", "-" }, StringSplitOptions.RemoveEmptyEntries);
					
					foreach (var qWord in queryWords)
					{
						if (string.IsNullOrWhiteSpace(qWord)) continue;
						if (nameWords.Any(nWord => nWord.StartsWith(qWord) || nWord.Contains(qWord)))
							return true;
					}
					
					return false;
				});

				if (doctor != null)
				{
					_logger.LogInformation("✅ Found doctor: {Name} (ID={Id})", doctor.FullName, doctor.Id);
				}
				else
				{
					_logger.LogInformation("❌ No doctor found matching: {Query}", query);
				}

				return doctor;
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Error finding doctor");
				return null;
			}
		}

		/// <summary>
		/// Tìm service theo tên (Fuzzy matching)
		/// VD: "trẻ hóa" khớp "Liệu trình trẻ hóa da"
		/// </summary>
		private async Task<ServiceEntity> FindServiceByNameAsync(string query)
		{
			try
			{
				var services = await _serviceRepository.FindByPredicate(s => !s.DeleteStatus);
				
				var queryLower = query.ToLower();
				var service = services.FirstOrDefault(s =>
				{
					var nameLower = s.ServiceName.ToLower();
					
					// Cách 1: Tên chứa query
					if (nameLower.Contains(queryLower))
						return true;
					
					// Cách 2: Bất kỳ từ nào từ query khớp với từ từ tên
					var nameWords = nameLower.Split(new[] { " ", "-" }, StringSplitOptions.RemoveEmptyEntries);
					var queryWords = queryLower.Split(new[] { " ", "-" }, StringSplitOptions.RemoveEmptyEntries);
					
					foreach (var qWord in queryWords)
					{
						if (string.IsNullOrWhiteSpace(qWord)) continue;
						if (nameWords.Any(nWord => nWord.StartsWith(qWord) || nWord.Contains(qWord)))
							return true;
					}
					
					return false;
				});

				if (service != null)
				{
					_logger.LogInformation("✅ Found service: {Name} (ID={Id})", service.ServiceName, service.Id);
				}
				else
				{
					_logger.LogInformation("❌ No service found matching: {Query}", query);
				}

				return service;
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Error finding service");
				return null;
			}
		}

		/// <summary>
		/// Tìm product theo tên, loại danh mục, hoặc nhà cung cấp
		/// VD: 
		/// - "kem dưỡng" khớp "Kem dưỡng da cao cấp"
		/// - "gucci dưỡng da" khớp product của gucci (supplier)
		/// - "dưỡng da" khớp products trong category dưỡng da
		/// </summary>
		private async Task<ProductEntity> FindProductAsync(string query)
		{
			try
			{
				var products = await _productRepository.FindByPredicate(p => !p.DeleteStatus);
				
				if (!products.Any())
					return null;

				var queryLower = query.ToLower();
				var queryWords = queryLower.Split(new[] { " ", "-" }, StringSplitOptions.RemoveEmptyEntries)
					.Where(w => !string.IsNullOrWhiteSpace(w))
					.ToArray();

				// ✅ Tìm product theo nhiều tiêu chí:
				var product = products.FirstOrDefault(p =>
				{
					var productNameLower = (p.ProductName ?? "").ToLower();
					var supplierNameLower = (p.Supplier?.SupplierName ?? "").ToLower();
					var serviceTypeLower = (p.ServiceType?.ServiceTypeName ?? "").ToLower();

					// Tiêu chí 1: Tên sản phẩm chứa query
					if (!string.IsNullOrEmpty(productNameLower) && productNameLower.Contains(queryLower))
						return true;

					// Tiêu chí 2: Tên nhà cung cấp chứa query (VD: "gucci")
					if (!string.IsNullOrEmpty(supplierNameLower) && supplierNameLower.Contains(queryLower))
						return true;

					// Tiêu chí 3: Bất kỳ từ nào từ query khớp với từ từ tên product
					if (!string.IsNullOrEmpty(productNameLower))
					{
						var productWords = productNameLower.Split(new[] { " ", "-" }, StringSplitOptions.RemoveEmptyEntries);
						foreach (var qWord in queryWords)
						{
							if (productWords.Any(pWord => pWord.StartsWith(qWord) || pWord.Contains(qWord)))
								return true;
						}
					}

					// Tiêu chí 4: Bất kỳ từ nào từ query khớp với loại danh mục (VD: "dưỡng da")
					if (!string.IsNullOrEmpty(serviceTypeLower))
					{
						var serviceTypeWords = serviceTypeLower.Split(new[] { " ", "-" }, StringSplitOptions.RemoveEmptyEntries);
						foreach (var qWord in queryWords)
						{
							if (serviceTypeWords.Any(sWord => sWord.StartsWith(qWord) || sWord.Contains(qWord)))
								return true;
						}
					}

					// Tiêu chí 5: Bất kỳ từ nào từ query khớp với tên nhà cung cấp
					if (!string.IsNullOrEmpty(supplierNameLower))
					{
						var supplierWords = supplierNameLower.Split(new[] { " ", "-" }, StringSplitOptions.RemoveEmptyEntries);
						foreach (var qWord in queryWords)
						{
							if (supplierWords.Any(sWord => sWord.StartsWith(qWord) || sWord.Contains(qWord)))
								return true;
						}
					}

					return false;
				});

				if (product != null)
				{
					_logger.LogInformation("✅ Found product: {Name} (ID={Id}), Supplier: {Supplier}", 
						product.ProductName, product.Id, product.Supplier?.SupplierName ?? "N/A");
				}
				else
				{
					_logger.LogInformation("❌ No product found matching: {Query}", query);
				}

				return product;
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Error finding product");
				return null;
			}
		}

		/// <summary>
		/// Xây dựng system prompt chứa hướng dẫn và danh sách tools
		/// </summary>
		private string BuildSystemPrompt(AIToolsListResponse toolsList)
		{
			var toolsJson = System.Text.Json.JsonSerializer.Serialize(
				toolsList.Tools,
				new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

			return $@"{toolsList.SystemPrompt}

DANH SÁCH CÁC TOOL CÓ SẴN:
{toolsJson}

FORMAT RESPONSE:
{toolsList.ResponseFormat}

HƯỚNG DẪN:
- Phân tích kỹ yêu cầu của người dùng
- Query đã được pre-processed, các doctor/service/product được đánh dấu với [Doctor: ID=X, Name=Y], [Service: ID=X, Name=Y] hoặc [Product: ID=X, Name=Y]
- Sử dụng IDs từ các đánh dấu này để điền vào params
- Chọn tool phù hợp nhất dựa trên yêu cầu
- Điền đầy đủ tất cả tham số bắt buộc
- Trả về JSON hợp lệ
- Nếu không chắc, hãy chọn tool có liên quan nhất hoặc hỏi làm rõ";
		}

		/// <summary>
		/// ✅ Request model cho API CallLLM
		/// </summary>
		public class CallLLMRequest
		{
			public string UserMessage { get; set; }
			public List<AIConversationMessage> ConversationHistory { get; set; }
		}

		/// <summary>
		/// ✅ Request model cho API ParseLLMResponse
		/// </summary>
		public class ParseLLMResponseRequest
		{
			public string LLMResponse { get; set; }
		}
	}
}