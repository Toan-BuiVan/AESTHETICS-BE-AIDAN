using Aesthetics.Data.AestheticsInterfaces;
using Aesthetics.Data.RepositoryInterfaces;
using Aesthetics.Entities.Entities;
using Aesthetics.Entities.Enum;
using Aesthetics.Entities.Models.RequestModel;
using Aesthetics.Entities.Models.ResponseModel;
using LinqKit;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Data.AestheticsServices
{
    public class ServiceTypeService : IServiceTypeService
    {
		private readonly ILogger<ServiceTypeService> _logger;
		private readonly IServiceTypeRepository _serviceTypeRepository;

		public ServiceTypeService(ILogger<ServiceTypeService> logger, IServiceTypeRepository serviceTypeRepository)
		{
			_logger = logger;
			_serviceTypeRepository = serviceTypeRepository;
		}

		public async Task<bool> create(RequestServiceType serviceType)
		{
			try
			{
				var existingServiceType = await _serviceTypeRepository.GetByName(serviceType.ServiceTypeName);
				if (existingServiceType != null)
				{
					_logger.LogWarning("Create ServiceType failed: ServiceTypeName {ServiceTypeName}", serviceType.ServiceTypeName);
					return false;
				}

				var entity = new ServiceTypeEntity
				{
					ServiceTypeName = serviceType.ServiceTypeName,
					ServiceCategory = (int)serviceType.ServiceCategory,
					Description = serviceType.Description.Trim(),
					DeleteStatus = false
				};

				var created = await _serviceTypeRepository.CreateEntity(entity);
				if (!created)
				{
					_logger.LogError("Create ServiceType failed at repository level: {ServiceTypeName}", serviceType.ServiceTypeName);
					return false;
				}
				var clinic = new ClinicEntity
				{
					ServiceTypeId = entity.Id,
					ClinicName = "Phòng khám" + serviceType.ServiceTypeName,
					ClinicStatus = true
				};

				_logger.LogInformation("Create ServiceType success: {ServiceTypeName}", serviceType.ServiceTypeName);
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Create ServiceType exception: ServiceTypeName {ServiceTypeName}", serviceType.ServiceTypeName);
				return false;
			}
		}

		public async Task<bool> delete(DeleteServiceType serviceType)
		{
			try
			{
				_logger.LogInformation("Start deleting ServiceType");
				var existingServiceType = await _serviceTypeRepository.GetById(serviceType.Id);
				if (existingServiceType == null)
				{
					_logger.LogWarning("Delete ServiceType failed: Not found with Id {Id}", serviceType.Id);
					return false;
				}

				var deleted = await _serviceTypeRepository.DeleteRangeEntitiesStatus(existingServiceType);
				if (!deleted)
				{
					_logger.LogError("Delete ServiceType failed at repository level: Id {Id}", serviceType.Id);
					return false;
				}

				_logger.LogInformation("Delete ServiceType success: Id {Id}", serviceType.Id);
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Delete ServiceType exception: Id {Id}", serviceType.Id);
				return false;
			}
		}

		public async Task<BaseDataCollection<ServiceTypeEntity>> getlist(ServiceTypeGet searchServiceType)
		{
			try
			{
				Expression<Func<ServiceTypeEntity, bool>> predicate = x => true;

				if (searchServiceType.Id.HasValue)
				{
					predicate = predicate.And(x => x.Id == searchServiceType.Id.Value);
				}

				if (!string.IsNullOrWhiteSpace(searchServiceType.ServiceTypeName))
				{
					predicate = predicate.And(x => x.ServiceTypeName.ToLower().Contains(searchServiceType.ServiceTypeName.ToLower()));
				}

				if (searchServiceType.ServiceCategory.HasValue)
				{
					predicate = predicate.And(x =>
						x.ServiceCategory == (int)searchServiceType.ServiceCategory.Value);
				}

				var allMatching = await _serviceTypeRepository.FindByPredicate(predicate);
				var totalCount = allMatching.Count;

				var pagedData = allMatching
					.OrderBy(x => x.ServiceTypeName)
					.Skip((searchServiceType.PageNo - 1) * searchServiceType.PageSize)
					.Take(searchServiceType.PageSize)
					.ToList();

				return new BaseDataCollection<ServiceTypeEntity>(pagedData, totalCount, searchServiceType.PageNo, searchServiceType.PageSize);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GetList ServiceType exception");
				return new BaseDataCollection<ServiceTypeEntity>(null, 0, searchServiceType.PageNo, searchServiceType.PageSize);
			}
		}

		public async Task<bool> update(UpdateServiceType serviceType)
		{
			try
			{
				var existingServiceType = await _serviceTypeRepository.GetById(serviceType.Id);
				if (existingServiceType == null)
				{
					_logger.LogWarning("Update ServiceType failed: Not found with Id {Id}", serviceType.Id);
					return false;
				}

				if (!string.IsNullOrEmpty(serviceType.ServiceTypeName) && existingServiceType.ServiceTypeName != serviceType.ServiceTypeName)
				{
					var duplicate = await _serviceTypeRepository.GetByName(serviceType.ServiceTypeName);
					if (duplicate != null)
					{
						_logger.LogWarning("Update ServiceType failed: Duplicate ServiceTypeName {ServiceTypeName}", serviceType.ServiceTypeName);
						return false;
					}
				}

				if (!string.IsNullOrEmpty(serviceType.ServiceTypeName))
					existingServiceType.ServiceTypeName = serviceType.ServiceTypeName;

				if (serviceType.ServiceCategory.HasValue)
					existingServiceType.ServiceCategory = (int)serviceType.ServiceCategory.Value;

				if (!string.IsNullOrEmpty(serviceType.Description))
					existingServiceType.Description = serviceType.Description.Trim();

				var updated = await _serviceTypeRepository.UpdateEntity(existingServiceType);
				if (!updated)
				{
					_logger.LogError("Update ServiceType failed at repository level: Id {Id}", serviceType.Id);
					return false;
				}

				_logger.LogInformation("Update ServiceType success: Id {Id}", serviceType.Id);
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Update ServiceType exception: Id {Id}", serviceType.Id);
				return false;
			}
		}
	}
}
