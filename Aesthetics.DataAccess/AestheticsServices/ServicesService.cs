using Aesthetics.Data.AestheticsInterfaces;
using Aesthetics.Data.AestheticsInterfaces.ICommonService;
using Aesthetics.Data.RepositoryInterfaces;
using Aesthetics.Data.RepositoryServices;
using Aesthetics.Entities.Entities;
using Aesthetics.Entities.Enum;
using Aesthetics.Entities.Models.RequestModel;
using Aesthetics.Entities.Models.ResponseModel;
using ClosedXML.Excel;
using LinqKit;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using XAct;

namespace Aesthetics.Data.AestheticsServices
{
	public class ServicesService : IServicesService
	{
		private readonly ILogger<ServicesService> _logger;
		private readonly IServiceRepository _serviceRepository;
		private readonly IServiceTypeRepository _serviceTypeRepository;
		private ICommonService _commonService;
		private readonly ITreatmentPlanRepository _treatmentPlanRepository;
		private readonly ITreatmentSessionRepository _treatmentSessionRepository;

		public ServicesService(ILogger<ServicesService> logger
			, IServiceRepository serviceRepository
			, ICommonService commonService
			, ITreatmentPlanRepository treatmentPlanRepository
			, ITreatmentSessionRepository treatmentSessionRepository
			, IServiceTypeRepository serviceTypeRepository)
		{
			_logger = logger;
			_serviceRepository = serviceRepository;
			_commonService = commonService;
			_treatmentPlanRepository = treatmentPlanRepository;
			_treatmentSessionRepository = treatmentSessionRepository;
			_serviceTypeRepository = serviceTypeRepository;
		}
		public async Task<CreateServiceResponseModel> create(CreateService service)
		{
			try
			{
				if (service == null)
				{
					_logger.LogWarning("CreateService request is null.");
					return new CreateServiceResponseModel
					{
						Success = false,
						ServiceId = null,
						Message = "CreateService request is null."
					};
				}
				string processedImage = service.ServiceImage;
				if (!string.IsNullOrEmpty(service.ServiceImage))
				{
					processedImage = await _commonService.BaseProcessingFunction64(service.ServiceImage);
				}
				var serviceName = await _serviceRepository.GetByName(service.ServiceName);
				if (serviceName != null)
				{
					_logger.LogWarning("Create Service failed: Service with name '{ServiceName}' already exists.", service.ServiceName);
					return new CreateServiceResponseModel
					{
						Success = false,
						ServiceId = null,
						Message = $"Service with name '{service.ServiceName}' already exists."
					};
				}
				var newService = new ServiceEntity
				{
					ServiceTypeId = service.ServiceTypeId,
					ServiceName = service.ServiceName,
					Description = service.Description,
					ServiceImage = processedImage,
					Price = service.Price ?? 0,
					Duration = service.Duration ?? 0,
					IsCourse = service.IsCourse ?? false
				};
				await _serviceRepository.CreateEntity(newService);
				_logger.LogInformation("Create Service success: ServiceId {ServiceId}", newService.Id);
				return new CreateServiceResponseModel
				{
					Success = true,
					ServiceId = newService.Id,
					Message = "Service created successfully."
				};
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error creating service: {ServiceName}", service.ServiceName);
				return new CreateServiceResponseModel
				{
					Success = false,
					ServiceId = null,
					Message = $"Error creating service: {ex.Message}"
				};
			}
		}

		public async Task<bool> delete(DeleteService service)
		{
			try
			{
				_logger.LogInformation("Start deleting Service");
				var services = await _serviceRepository.GetByIdForDelete(service.Id.Value);
				if (services == null)
				{
					_logger.LogWarning("Delete Service failed: Not found with Id {Id}", service.Id);
					return false;
				}
				var deleted = await _serviceRepository.DeleteRangeEntitiesStatus(services);
				if (!deleted)
				{
					_logger.LogError("Delete Service failed at repository level: Id {Id}", service.Id);
					return false;
				}
				_logger.LogInformation("Delete Service success: Id {Id}", service.Id);
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Delete Service exception: Id {Id}", service.Id);
				return false;
			}
		}

		public async Task<bool> update(UpdateService service)
		{
			try
			{
				if (service == null || !service.Id.HasValue)
				{
					_logger.LogWarning("UpdateService request invalid.");
					return false;
				}

				var existingService = await _serviceRepository.GetById(service.Id.Value);

				if (existingService == null)
				{
					_logger.LogWarning("Update Service failed: Not found with Id {Id}", service.Id);
					return false;
				}

				// ✅ Lưu giữ giá trị cũ để so sánh
				bool? oldIsCourse = existingService.IsCourse;
				decimal? oldPrice = existingService.Price;
				int? oldDuration = existingService.Duration;

				if (!string.IsNullOrWhiteSpace(service.ServiceName)
					&& existingService.ServiceName != service.ServiceName)
				{
					var duplicate = await _serviceRepository.GetByName(service.ServiceName);
					if (duplicate != null)
					{
						_logger.LogWarning("Update Service failed: Duplicate ServiceName {ServiceName}", service.ServiceName);
						return false;
					}
					existingService.ServiceName = service.ServiceName.Trim();
				}

				if (service.ServiceTypeId.HasValue)
					existingService.ServiceTypeId = service.ServiceTypeId.Value;

				if (!string.IsNullOrWhiteSpace(service.Description))
					existingService.Description = service.Description.Trim();

				if (service.Price.HasValue)
					existingService.Price = service.Price.Value;

				if (service.Duration.HasValue)
					existingService.Duration = service.Duration.Value;

				if (service.IsCourse.HasValue)
					existingService.IsCourse = service.IsCourse.Value;

				if (!string.IsNullOrWhiteSpace(service.ServiceImage)
					&& existingService.ServiceImage != service.ServiceImage)
				{
					var processedImage = await _commonService.BaseProcessingFunction64(service.ServiceImage);
					existingService.ServiceImage = processedImage;
				}

				var updated = await _serviceRepository.UpdateEntity(existingService);

				if (!updated)
				{
					_logger.LogError("Update Service failed at repository level: Id {Id}", service.Id);
					return false;
				}

				// ✅ Xử lý thay đổi IsCourse
				await HandleTreatmentPlanByCourse(existingService.Id, oldIsCourse, existingService.IsCourse ?? false);

				// ✅ Xử lý thay đổi giá - tính toán lại giá TreatmentPlans
				if (oldPrice.HasValue && service.Price.HasValue && oldPrice != service.Price)
				{
					await UpdateTreatmentPlanPricesByService(existingService.Id, oldPrice.Value, service.Price.Value);
				}

				// ✅ Xử lý thay đổi thời lượng - cập nhật Duration của TreatmentSessions
				if (oldDuration.HasValue && service.Duration.HasValue && oldDuration != service.Duration)
				{
					await UpdateTreatmentSessionDurationsByService(existingService.Id, service.Duration.Value);
				}

				_logger.LogInformation("Update Service success: Id {Id}, PriceChanged: {PriceChanged}, DurationChanged: {DurationChanged}",
					service.Id,
					oldPrice != service.Price,
					oldDuration != service.Duration);
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Update Service exception: Id {Id}", service.Id);
				return false;
			}
		}

		/// <summary>
		/// ✅ Cập nhật giá của tất cả TreatmentPlans khi giá Service thay đổi
		/// Tính toán lại giá dựa trên tỉ lệ thay đổi giữa giá cũ và giá mới
		/// </summary>
		private async Task UpdateTreatmentPlanPricesByService(int serviceId, decimal oldPrice, decimal newPrice)
		{
			try
			{
				_logger.LogInformation("UpdateTreatmentPlanPricesByService started: ServiceId {ServiceId}, OldPrice {OldPrice} → NewPrice {NewPrice}",
					serviceId, oldPrice, newPrice);

				// ✅ Lấy tất cả TreatmentPlans của Service này (chưa bị xóa)
				var treatmentPlans = await _treatmentPlanRepository
					.FindByPredicate(x => x.ServiceId == serviceId && x.DeleteStatus != true);

				if (!treatmentPlans.Any())
				{
					_logger.LogInformation("Không tìm thấy TreatmentPlans nào cho ServiceId {ServiceId}", serviceId);
					return;
				}

				const decimal discountRate = 0.85m;
				var plansToUpdate = new List<TreatmentPlanEntity>();

				foreach (var plan in treatmentPlans)
				{
					if (oldPrice > 0)
					{
						// ✅ Tính tỉ lệ thay đổi giá
						var priceRatio = newPrice / oldPrice;
						var newPlanPrice = (plan.Price ?? 0) * priceRatio;

						// ✅ Làm tròn về 1000 (theo quy tắc của hệ thống)
						newPlanPrice = Math.Floor(newPlanPrice / 1000) * 1000;

						plan.Price = newPlanPrice;
						plansToUpdate.Add(plan);

						_logger.LogInformation("Cập nhật giá TreatmentPlan: PlanId {PlanId}, OldPrice {OldPrice} → NewPrice {NewPrice}",
							plan.Id, (plan.Price ?? 0) / priceRatio, newPlanPrice);
					}
				}

				if (plansToUpdate.Any())
				{
					var updateSuccess = await _treatmentPlanRepository.UpdateRangeEntities(plansToUpdate);

					if (updateSuccess)
					{
						_logger.LogInformation("UpdateTreatmentPlanPricesByService thành công: Cập nhật {Count} gói liệu trình cho ServiceId {ServiceId}",
							plansToUpdate.Count, serviceId);
					}
					else
					{
						_logger.LogError("UpdateTreatmentPlanPricesByService thất bại ở Repository: ServiceId {ServiceId}", serviceId);
					}
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "UpdateTreatmentPlanPricesByService exception: ServiceId {ServiceId}", serviceId);
			}
		}

		/// <summary>
		/// ✅ Cập nhật Duration của tất cả TreatmentSessions khi Duration của Service thay đổi
		/// </summary>
		private async Task UpdateTreatmentSessionDurationsByService(int serviceId, int newDuration)
		{
			try
			{
				_logger.LogInformation("UpdateTreatmentSessionDurationsByService started: ServiceId {ServiceId}, NewDuration {NewDuration} phút",
					serviceId, newDuration);

				// ✅ Lấy tất cả TreatmentPlans của Service này
				var treatmentPlans = await _treatmentPlanRepository
					.FindByPredicate(x => x.ServiceId == serviceId && x.DeleteStatus != true);

				if (!treatmentPlans.Any())
				{
					_logger.LogInformation("Không tìm thấy TreatmentPlans nào cho ServiceId {ServiceId}", serviceId);
					return;
				}

				var planIds = treatmentPlans.Select(x => x.Id).ToList();

				// ✅ Lấy tất cả TreatmentSessions của các Plans này
				var treatmentSessions = await _treatmentSessionRepository
					.FindByPredicate(x => planIds.Contains(x.TreatmentPlanId ?? 0) && x.DeleteStatus != true);

				if (!treatmentSessions.Any())
				{
					_logger.LogInformation("Không tìm thấy TreatmentSessions nào cho ServiceId {ServiceId}", serviceId);
					return;
				}

				var sessionsToUpdate = new List<TreatmentSessionEntity>();

				foreach (var session in treatmentSessions)
				{
					session.Duration = newDuration;
					sessionsToUpdate.Add(session);

					_logger.LogInformation("Cập nhật Duration: SessionId {SessionId}, Duration {Duration} phút",
						session.Id, newDuration);
				}

				if (sessionsToUpdate.Any())
				{
					var updateSuccess = await _treatmentSessionRepository.UpdateRangeEntities(sessionsToUpdate);

					if (updateSuccess)
					{
						_logger.LogInformation("UpdateTreatmentSessionDurationsByService thành công: Cập nhật {Count} buổi điều trị cho ServiceId {ServiceId}",
							sessionsToUpdate.Count, serviceId);
					}
					else
					{
						_logger.LogError("UpdateTreatmentSessionDurationsByService thất bại ở Repository: ServiceId {ServiceId}", serviceId);
					}
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "UpdateTreatmentSessionDurationsByService exception: ServiceId {ServiceId}", serviceId);
			}
		}

		private async Task HandleTreatmentPlanByCourse(int serviceId, bool? oldIsCourse, bool? newIsCourse)
		{
			// ✅ So sánh bool? với bool?
			if (oldIsCourse == newIsCourse)
				return;

			var plans = await _treatmentPlanRepository
				.FindByPredicate(x => x.ServiceId == serviceId);

			// SINGLE → PACKAGE (newIsCourse == true)
			if (newIsCourse == true)  // ✅ Sửa
			{
				if (!plans.Any())
				{
					var newPlan = new TreatmentPlanEntity
					{
						ServiceId = serviceId,
						DeleteStatus = false
					};

					await _treatmentPlanRepository.CreateEntity(newPlan);

					var newSession = new TreatmentSessionEntity
					{
						TreatmentPlanId = newPlan.Id,
						DeleteStatus = false
					};

					await _treatmentSessionRepository.CreateEntity(newSession);
				}
				else
				{
					foreach (var plan in plans)
						plan.DeleteStatus = false;

					await _treatmentPlanRepository.UpdateRangeEntities(plans);

					var planIds = plans.Select(x => x.Id).ToList();

					var sessions = await _treatmentSessionRepository
						.FindByPredicate(x => planIds.Contains(x.TreatmentPlanId ?? 0));

					foreach (var session in sessions)
						session.DeleteStatus = false;

					await _treatmentSessionRepository.UpdateRangeEntities(sessions);
				}
			}

			// PACKAGE → SINGLE (newIsCourse == false)
			else if (newIsCourse == false)  // ✅ Sửa
			{
				if (!plans.Any())
					return;

				foreach (var plan in plans)
					plan.DeleteStatus = true;

				await _treatmentPlanRepository.UpdateRangeEntities(plans);

				var planIds = plans.Select(x => x.Id).ToList();

				var sessions = await _treatmentSessionRepository
					.FindByPredicate(x => planIds.Contains(x.TreatmentPlanId ?? 0));

				foreach (var session in sessions)
					session.DeleteStatus = true;

				await _treatmentSessionRepository.UpdateRangeEntities(sessions);
			}
		}

		public async Task<byte[]?> ExportToExcelAsync(exportservice exportService)
		{
			try
			{
				_logger.LogInformation("Starting ExportToExcelAsync with ClosedXML");

				// ✅ Chỉ filter DeleteStatus + ServiceIds
				Expression<Func<ServiceEntity, bool>> predicate = x => x.DeleteStatus != true;

				if (exportService.ServiceIds != null && exportService.ServiceIds.Any())
				{
					predicate = predicate.And(x => exportService.ServiceIds.Contains(x.Id));
					_logger.LogInformation("Exporting {Count} specific services by IDs", exportService.ServiceIds.Count);
				}
				else
				{
					_logger.LogInformation("Exporting all active services");
				}

				// Load tất cả record
				var allServices = await _serviceRepository.FindByPredicate(predicate);
				var allServicesList = allServices.ToList();

				if (!allServicesList.Any())
				{
					_logger.LogWarning("No services found for export");
					return CreateEmptyServiceExcel();
				}

				// Batch load ServiceType
				var serviceTypeIds = allServicesList
					.Where(x => x.ServiceTypeId.HasValue)
					.Select(x => x.ServiceTypeId.Value)
					.Distinct()
					.ToList();

				var serviceTypes = new Dictionary<int, ServiceTypeEntity>();
				if (serviceTypeIds.Any())
				{
					var typesList = await _serviceTypeRepository.FindByPredicate(x => serviceTypeIds.Contains(x.Id));
					serviceTypes = typesList.ToDictionary(x => x.Id, x => x);
				}

				// Gán navigation
				foreach (var svc in allServicesList)
				{
					if (svc.ServiceTypeId.HasValue && serviceTypes.TryGetValue(svc.ServiceTypeId.Value, out var st))
					{
						svc.ServiceType = st;
					}
				}

				// Sắp xếp
				var finalResults = allServicesList
					.OrderBy(x => x.Id)
					.ToList();

				_logger.LogDebug("Starting Excel export with ClosedXML");

				// ✅ Sử dụng ClosedXML thay vì EPPlus
				using (var workbook = new XLWorkbook())
				{
					var worksheet = workbook.Worksheets.Add("Services");

					// Headers
					worksheet.Cell(1, 1).Value = "Id";
					worksheet.Cell(1, 2).Value = "ServiceTypeName";
					worksheet.Cell(1, 3).Value = "ServiceName";
					worksheet.Cell(1, 4).Value = "Description";
					worksheet.Cell(1, 5).Value = "ServiceImage";
					worksheet.Cell(1, 6).Value = "Price";
					worksheet.Cell(1, 7).Value = "Duration";
					worksheet.Cell(1, 8).Value = "IsCourse";

					// ✅ Format header - Bold và màu xám
					var headerRow = worksheet.Row(1);
					headerRow.Style.Font.Bold = true;
					headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;

					// Data
					for (int i = 0; i < finalResults.Count; i++)
					{
						var row = i + 2;
						var item = finalResults[i];

						worksheet.Cell(row, 1).Value = item.Id;
						worksheet.Cell(row, 2).Value = item.ServiceType?.ServiceTypeName;
						worksheet.Cell(row, 3).Value = item.ServiceName;
						worksheet.Cell(row, 4).Value = item.Description;
						worksheet.Cell(row, 5).Value = item.ServiceImage;
						worksheet.Cell(row, 6).Value = item.Price;
						worksheet.Cell(row, 7).Value = item.Duration;
						worksheet.Cell(row, 8).Value = item.IsCourse ?? false;
					}

					// ✅ Auto fit columns
					worksheet.Columns().AdjustToContents();

					// Lưu vào memory stream
					using (var stream = new MemoryStream())
					{
						workbook.SaveAs(stream);
						_logger.LogInformation("Successfully exported {Count} services to Excel", finalResults.Count);
						return stream.ToArray();
					}
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Export Services to Excel exception");
				return null;
			}
		}

		private byte[] CreateEmptyServiceExcel()
		{
			try
			{
				using (var workbook = new XLWorkbook())
				{
					var ws = workbook.Worksheets.Add("Services");

					string[] headers =
					[
						"Id", "ServiceTypeName", "ServiceName", "Description",
						"ServiceImage", "Price", "Duration", "IsCourse"
					];

					// Headers
					for (int col = 0; col < headers.Length; col++)
					{
						ws.Cell(1, col + 1).Value = headers[col];
					}

					// Format header
					var headerRow = ws.Row(1);
					headerRow.Style.Font.Bold = true;
					headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;

					// Empty message
					ws.Cell(2, 1).Value = "No services found";
					ws.Range(2, 1, 2, headers.Length).Merge();
					ws.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
					ws.Cell(2, 1).Style.Font.Italic = true;

					// Auto fit
					ws.Columns().AdjustToContents();

					using (var stream = new MemoryStream())
					{
						workbook.SaveAs(stream);
						return stream.ToArray();
					}
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "CreateEmptyServiceExcel exception");
				return Array.Empty<byte>();
			}
		}

		public async Task<BaseDataCollection<ServiceResponseModel>> GetListAsync(ServiceGet service)
		{
			try
			{
				Expression<Func<ServiceEntity, bool>> predicate = x => x.DeleteStatus != true;

				if (service.Id.HasValue)
				{
					predicate = predicate.And(x => x.Id == service.Id.Value);
				}

				if (!string.IsNullOrWhiteSpace(service.ServiceName))
				{
					var name = service.ServiceName.ToLower();
					predicate = predicate.And(x => x.ServiceName != null &&
												  x.ServiceName.ToLower().Contains(name));
				}

				if (service.IsCourse.HasValue)
				{
					predicate = predicate.And(x => x.IsCourse == service.IsCourse.Value);
				}

				// Load tất cả record khớp predicate trước (giống product)
				var allMatching = await _serviceRepository.FindByPredicate(predicate);
				var allMatchingList = allMatching.ToList();

				// Lấy tất cả ServiceTypeId unique
				var serviceTypeIds = allMatchingList
					.Where(x => x.ServiceTypeId.HasValue)
					.Select(x => x.ServiceTypeId.Value)
					.Distinct()
					.ToList();

				// Batch load ServiceTypes
				var serviceTypes = new Dictionary<int, ServiceTypeEntity>();
				if (serviceTypeIds.Any())
				{
					var serviceTypesList = await _serviceTypeRepository.FindByPredicate(
						x => serviceTypeIds.Contains(x.Id));

					serviceTypes = serviceTypesList.ToDictionary(x => x.Id, x => x);
				}

				// Gán navigation property thủ công
				foreach (var svc in allMatchingList)
				{
					if (svc.ServiceTypeId.HasValue && serviceTypes.TryGetValue(svc.ServiceTypeId.Value, out var st))
					{
						svc.ServiceType = st;
					}
				}

				// Lọc thêm trong memory nếu có filter trên ServiceTypeName
				var filteredResults = allMatchingList.AsQueryable();

				if (service.ServiceTypeId.HasValue)
				{
					filteredResults = filteredResults.Where(x =>
						x.ServiceTypeId == service.ServiceTypeId.Value);
				}

				var finalResults = filteredResults.ToList();
				var totalCount = finalResults.Count;

				// Phân trang + project sang response model
				var pagedData = finalResults
					.OrderBy(x => x.ServiceName ?? string.Empty)
					.Skip((service.PageNo - 1) * service.PageSize)
					.Take(service.PageSize)
					.Select(x => new ServiceResponseModel
					{
						Id = x.Id,
						ServiceTypeId = x.ServiceTypeId,
						ServiceTypeName = x.ServiceType?.ServiceTypeName,
						ServiceName = x.ServiceName,
						Description = x.Description,
						ServiceImage = x.ServiceImage,
						Price = x.Price,
						Duration = x.Duration,
						IsCourse = x.IsCourse
					})
					.ToList();

				return new BaseDataCollection<ServiceResponseModel>(
					pagedData,
					totalCount,
					service.PageNo,
					service.PageSize);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GetList Service exception. Request: {@Request}", service);
				return new BaseDataCollection<ServiceResponseModel>(
					null,
					0,
					service?.PageNo ?? 1,
					service?.PageSize ?? 10);
			}
		}
	}
}
