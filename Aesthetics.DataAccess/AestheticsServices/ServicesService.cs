using Aesthetics.Data.AestheticsInterfaces;
using Aesthetics.Data.AestheticsInterfaces.ICommonService;
using Aesthetics.Data.RepositoryInterfaces;
using Aesthetics.Data.RepositoryServices;
using Aesthetics.Entities.Entities;
using Aesthetics.Entities.Enum;
using Aesthetics.Entities.Models.RequestModel;
using Aesthetics.Entities.Models.ResponseModel;
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
		public async Task<bool> create(CreateService service)
		{
			try
			{
				if (service == null)
				{
					_logger.LogWarning("CreateService request is null.");
					return false;
				}
				string processedImage = service.ServiceImage;
				if (!string.IsNullOrEmpty(service.ServiceImage))
				{
					processedImage = await _commonService.BaseProcessingFunction64(service.ServiceImage);
				}
				var serviceName = _serviceRepository.GetByName(service.ServiceName);
				if (serviceName != null)
				{
					_logger.LogWarning("Create Service failed: Service with name '{ServiceName}' already exists.", service.ServiceName);
					return false;
				}
				var newService = new ServiceEntity
				{
					ServiceTypeId = service.ServiceTypeId,
					ServiceName = service.ServiceName,
					Description = service.Description,
					ServiceImage = processedImage,
					Price = service.Price ?? 0,
					Duration = service.Duration ?? 0,
					IsCourse = (int)service.IsCourse
				};
				await _serviceRepository.CreateEntity(newService);
				if (newService.IsCourse == (int)EnumTypeCourse.Package)
				{
					var newPlan = new TreatmentPlanEntity
					{
						ServiceId = newService.Id,
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
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error creating service: {ServiceName}", service.ServiceName);
				return false;
			}
		}

		public async Task<bool> delete(DeleteService service)
		{
			try
			{
				_logger.LogInformation("Start deleting Service");
				var existingService = await _serviceRepository.GetById(service.Id.Value);
				if (existingService == null)
				{
					_logger.LogWarning("Delete Service failed: Not found with Id {Id}", service.Id);
					return false;
				}
				var deleted = await _serviceRepository.DeleteRangeEntitiesStatus(existingService);
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

				int oldIsCourse = (int)existingService.IsCourse;

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
					existingService.IsCourse = (int)service.IsCourse.Value;

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

				await HandleTreatmentPlanByCourse(existingService.Id, oldIsCourse, existingService.IsCourse ?? 0);
				_logger.LogInformation("Update Service success: Id {Id}", service.Id);
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Update Service exception: Id {Id}", service.Id);
				return false;
			}
		}

		private async Task HandleTreatmentPlanByCourse(int serviceId, int oldIsCourse, int newIsCourse)
		{
			if (oldIsCourse == newIsCourse)
				return;

			var plans = await _treatmentPlanRepository
				.FindByPredicate(x => x.ServiceId == serviceId);


			if (newIsCourse == (int)EnumTypeCourse.Package)
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

			// PACKAGE → SINGLE
			else if (newIsCourse == (int)EnumTypeCourse.Single)
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
				_logger.LogInformation("Start exporting Services to Excel. Filters: {@Filters}", exportService);

				// Chỉ filter DeleteStatus + ServiceIds (theo đúng class của bạn)
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

				// Batch load ServiceType (giống Product)
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
					.OrderBy(x => x.ServiceName ?? string.Empty)
					.ToList();

				using var package = new ExcelPackage();
				var ws = package.Workbook.Worksheets.Add("Services");

				// Headers
				string[] headers =
				[
					"Id", "ServiceTypeName", "ServiceName", "Description",
					"ServiceImage", "Price", "Duration", "IsCourse"
				];

				for (int col = 0; col < headers.Length; col++)
				{
					ws.Cells[1, col + 1].Value = headers[col];
				}

				// Data
				for (int i = 0; i < finalResults.Count; i++)
				{
					var row = i + 2;
					var item = finalResults[i];

					ws.Cells[row, 1].Value = item.Id;
					ws.Cells[row, 2].Value = item.ServiceType?.ServiceTypeName;
					ws.Cells[row, 3].Value = item.ServiceName;
					ws.Cells[row, 4].Value = item.Description;
					ws.Cells[row, 5].Value = item.ServiceImage;
					ws.Cells[row, 6].Value = item.Price;
					ws.Cells[row, 7].Value = item.Duration;
					ws.Cells[row, 8].Value = item.IsCourse;
				}

				// Format giống Product
				ws.Cells[ws.Dimension.Address].AutoFitColumns();

				using (var range = ws.Cells[1, 1, 1, headers.Length])
				{
					range.Style.Font.Bold = true;
					range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
					range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
					range.Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
					range.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
					range.Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
					range.Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
				}

				_logger.LogInformation("Successfully exported {Count} services to Excel", finalResults.Count);
				return package.GetAsByteArray();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Export Services to Excel exception. Filters: {@Filters}", exportService);
				return null;
			}
		}

		private byte[] CreateEmptyServiceExcel()
		{
			using var package = new ExcelPackage();
			var ws = package.Workbook.Worksheets.Add("Services");

			string[] headers =
			[
				"Id", "ServiceTypeName", "ServiceName", "Description",
				"ServiceImage", "Price", "Duration", "IsCourse"
			];

			for (int col = 0; col < headers.Length; col++)
			{
				ws.Cells[1, col + 1].Value = headers[col];
			}

			ws.Cells[2, 1].Value = "No services found";
			ws.Cells[$"A2:{(char)('A' + headers.Length - 1)}2"].Merge = true;
			ws.Cells[2, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
			ws.Cells[2, 1].Style.Font.Italic = true;

			using (var range = ws.Cells[1, 1, 1, headers.Length])
			{
				range.Style.Font.Bold = true;
				range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
				range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
			}

			ws.Cells[ws.Dimension.Address].AutoFitColumns();
			return package.GetAsByteArray();
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
					var name = service.ServiceName.Trim().ToLowerInvariant();
					predicate = predicate.And(x => x.ServiceName != null &&
												  x.ServiceName.ToLowerInvariant().Contains(name));
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

				if (!string.IsNullOrWhiteSpace(service.ServiceTypeName))
				{
					var typeName = service.ServiceTypeName.Trim().ToLowerInvariant();
					filteredResults = filteredResults.Where(x => x.ServiceType != null &&
																x.ServiceType.ServiceTypeName != null &&
																x.ServiceType.ServiceTypeName.ToLowerInvariant().Contains(typeName));
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
