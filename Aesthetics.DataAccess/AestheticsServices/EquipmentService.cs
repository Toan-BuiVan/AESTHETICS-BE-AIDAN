using Aesthetics.Data.AestheticsInterfaces;
using Aesthetics.Data.RepositoryInterfaces;
using Aesthetics.Entities.Entities;
using Aesthetics.Entities.Models.RequestModel;
using Aesthetics.Entities.Models.ResponseModel;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Data.AestheticsServices
{
    public class EquipmentService : IEquipmentService
	{
		private readonly ILogger<EquipmentService> _logger;
		private readonly IEquipmentRepository _equipmentRepository;

		public EquipmentService(ILogger<EquipmentService> logger, IEquipmentRepository equipmentRepository)
		{
			_logger = logger;
			_equipmentRepository = equipmentRepository;
		}

		public async Task<bool> create(RequestEquipment equipment)
		{
			try
			{
				var existingEquipment = await _equipmentRepository.GetByName(equipment.EquipmentName);
				if (existingEquipment != null)
				{
					_logger.LogWarning("Create Equipment failed: EquipmentName {EquipmentName}", equipment.EquipmentName);
					return false;
				}

				var entity = new EquipmentEntity
				{
					EquipmentName = equipment.EquipmentName.Trim(),
					ClinicId = equipment.ClinicId,
					Status = "Active",
					DeleteStatus = false
				};

				var created = await _equipmentRepository.CreateEntity(entity);
				if (!created)
				{
					_logger.LogError("Create Equipment failed at repository level: {EquipmentName}", equipment.EquipmentName);
					return false;
				}

				_logger.LogInformation("Create Equipment success: {EquipmentName}", equipment.EquipmentName);
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Create Equipment exception: EquipmentName {EquipmentName}", equipment.EquipmentName);
				return false;
			}
		}

		public async Task<bool> delete(DeleteEquipment equipment)
		{
			try
			{
				_logger.LogInformation("Start deleting Equipment");

				var existingEquipment = await _equipmentRepository.GetById(equipment.Id);
				if (existingEquipment == null)
				{
					_logger.LogWarning("Delete Equipment failed: Not found with Id {Id}", equipment.Id);
					return false;
				}

				var deleted = await _equipmentRepository.DeleteRangeEntitiesStatus(existingEquipment);
				if (!deleted)
				{
					_logger.LogError("Delete Equipment failed at repository level: Id {Id}", equipment.Id);
					return false;
				}

				_logger.LogInformation("Delete Equipment success: Id {Id}", equipment.Id);
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Delete Equipment exception: Id {Id}", equipment.Id);
				return false;
			}
		}

		public async Task<BaseDataCollection<EquipmentEntity>> getlist(EquipmentGet searchEquipment)
		{
			try
			{
				Expression<Func<EquipmentEntity, bool>> predicate = x => x.DeleteStatus != true;

				if (!string.IsNullOrWhiteSpace(searchEquipment.EquipmentName))
				{
					predicate = x => x.EquipmentName.ToLower()
						.Contains(searchEquipment.EquipmentName.ToLower());
				}

				if (searchEquipment.ClinicId.HasValue)
				{
					var clinicId = searchEquipment.ClinicId.Value;
					predicate = x => x.ClinicId == clinicId;
				}

				if (searchEquipment.ServiceTypeId.HasValue)
				{
					var serviceTypeId = searchEquipment.ServiceTypeId.Value;
					predicate = x => x.Clinic != null && x.Clinic.ServiceTypeId == serviceTypeId;
				}

				var allMatching = await _equipmentRepository.FindByPredicate(predicate);
				var totalCount = allMatching.Count;

				var pagedData = allMatching
					.OrderBy(x => x.EquipmentName)
					.Skip((searchEquipment.PageNo - 1) * searchEquipment.PageSize)
					.Take(searchEquipment.PageSize)
					.ToList();

				return new BaseDataCollection<EquipmentEntity>(
					pagedData,
					totalCount,
					searchEquipment.PageNo,
					searchEquipment.PageSize
				);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GetList Equipment exception");
				return new BaseDataCollection<EquipmentEntity>(
					null,
					0,
					searchEquipment.PageNo,
					searchEquipment.PageSize
				);
			}
		}

		public async Task<bool> update(UpdateEquipment equipment)
		{
			try
			{
				var existingEquipment = await _equipmentRepository.GetById(equipment.Id);
				if (existingEquipment == null)
				{
					_logger.LogWarning("Update Equipment failed: Not found with Id {Id}", equipment.Id);
					return false;
				}

				if (!string.IsNullOrWhiteSpace(equipment.EquipmentName) &&
					existingEquipment.EquipmentName != equipment.EquipmentName)
				{
					var duplicate = await _equipmentRepository.GetByName(equipment.EquipmentName);
					if (duplicate != null)
					{
						_logger.LogWarning("Update Equipment failed: Duplicate EquipmentName {EquipmentName}", equipment.EquipmentName);
						return false;
					}

					existingEquipment.EquipmentName = equipment.EquipmentName.Trim();
				}

				if (equipment.ClinicId.HasValue)
				{
					existingEquipment.ClinicId = equipment.ClinicId.Value;
				}

				if (!string.IsNullOrWhiteSpace(equipment.Status))
				{
					existingEquipment.Status = equipment.Status;
				}

				var updated = await _equipmentRepository.UpdateEntity(existingEquipment);
				if (!updated)
				{
					_logger.LogError("Update Equipment failed at repository level: Id {Id}", equipment.Id);
					return false;
				}

				_logger.LogInformation("Update Equipment success: Id {Id}", equipment.Id);
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Update Equipment exception: Id {Id}", equipment.Id);
				return false;
			}
		}
	}
}
