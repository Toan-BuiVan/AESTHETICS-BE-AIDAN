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
    public class ClinicStaffService : IClinicStaffService
	{
		private readonly ILogger<ClinicStaffService> _logger;
		private readonly IClinicStaffRepository _clinicStaffRepository;

		public ClinicStaffService(ILogger<ClinicStaffService> logger, IClinicStaffRepository clinicStaffRepository)
		{
			_logger = logger;
			_clinicStaffRepository = clinicStaffRepository;
		}

		public async Task<bool> create(CreateClinicStaff clinicStaff)
		{
			try
			{
				if (!clinicStaff.ClinicId.HasValue || !clinicStaff.StaffId.HasValue)
				{
					_logger.LogWarning("Create ClinicStaff failed: Missing ClinicId or StaffId");
					return false;
				}
				var isStaff = await _clinicStaffRepository.CheckStaffId(clinicStaff.StaffId.Value);
				if (isStaff)
				{
					_logger.LogWarning(
					"Create ClinicStaff failed: StaffId {StaffId} already exists in another clinic.",
					clinicStaff.StaffId
					);
					return false;
				}
				var entity = new ClinicStaffEntity
				{
					ClinicId = clinicStaff.ClinicId.Value,
					StaffId = clinicStaff.StaffId.Value,
					DeleteStatus = false
				};
				var created = await _clinicStaffRepository.CreateEntity(entity);
				if (!created)
				{
					_logger.LogError("Create ClinicStaff failed at repository level: ClinicId {ClinicId}, StaffId {StaffId}", clinicStaff.ClinicId, clinicStaff.StaffId);
					return false;
				}
				_logger.LogInformation("Create ClinicStaff success: ClinicId {ClinicId}, StaffId {StaffId}", clinicStaff.ClinicId, clinicStaff.StaffId);
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Create ClinicStaff exception: ClinicId {ClinicId}, StaffId {StaffId}", clinicStaff.ClinicId, clinicStaff.StaffId);
				return false;
			}
		}

		public async Task<bool> delete(DeleteClinicStaff clinicStaff)
		{
			try
			{
				_logger.LogInformation("Start deleting ClinicStaff");
				if (!clinicStaff.Id.HasValue)
				{
					_logger.LogWarning("Delete ClinicStaff failed: Missing Id");
					return false;
				}
				var existingClinicStaff = await _clinicStaffRepository.GetById(clinicStaff.Id.Value);
				if (existingClinicStaff == null)
				{
					_logger.LogWarning("Delete ClinicStaff failed: Not found with Id {Id}", clinicStaff.Id);
					return false;
				}
				var deleted = await _clinicStaffRepository.DeleteRangeEntitiesStatus(existingClinicStaff);
				if (!deleted)
				{
					_logger.LogError("Delete ClinicStaff failed at repository level: Id {Id}", clinicStaff.Id);
					return false;
				}
				_logger.LogInformation("Delete ClinicStaff success: Id {Id}", clinicStaff.Id);
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Delete ClinicStaff exception: Id {Id}", clinicStaff.Id);
				return false;
			}
		}

		public async Task<BaseDataCollection<ClinicStaffEntity>> getlist(GetClinicStaff searchClinicStaff)
		{
			try
			{
				Expression<Func<ClinicStaffEntity, bool>> predicate = x => x.DeleteStatus != true;
				if (searchClinicStaff.Id.HasValue)
				{
					predicate = x => x.Id == searchClinicStaff.Id.Value && x.DeleteStatus != true;
				}
				else if (searchClinicStaff.ClinicId.HasValue)
				{
					predicate = x => x.ClinicId == searchClinicStaff.ClinicId.Value && x.DeleteStatus != true;
				}
				var allMatching = await _clinicStaffRepository.FindByPredicate(predicate);
				var totalCount = allMatching.Count;
				var pagedData = allMatching
				.OrderBy(x => x.Id)
				.Skip((searchClinicStaff.PageNo - 1) * searchClinicStaff.PageSize)
				.Take(searchClinicStaff.PageSize)
				.ToList();
				return new BaseDataCollection<ClinicStaffEntity>(
				pagedData,
				totalCount,
				searchClinicStaff.PageNo,
				searchClinicStaff.PageSize
				);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GetList ClinicStaff exception");
				return new BaseDataCollection<ClinicStaffEntity>(
				null,
				0,
				searchClinicStaff.PageNo,
				searchClinicStaff.PageSize
				);
			}
		}

		public async Task<bool> update(UpdateClinicStaff clinicStaff)
		{
			try
			{
				if (!clinicStaff.Id.HasValue)
				{
					_logger.LogWarning("Update ClinicStaff failed: Missing Id");
					return false;
				}
				var existingClinicStaff = await _clinicStaffRepository.GetById(clinicStaff.Id.Value);
				if (existingClinicStaff == null)
				{
					_logger.LogWarning("Update ClinicStaff failed: Not found with Id {Id}", clinicStaff.Id);
					return false;
				}
				if (clinicStaff.StaffId.HasValue)
				{
					var isStaff = await _clinicStaffRepository.CheckStaffId(clinicStaff.StaffId.Value);
					if (isStaff)
					{
						_logger.LogWarning(
						"Create ClinicStaff failed: StaffId {StaffId} already exists in another clinic.",
						clinicStaff.StaffId
						);
						return false;
					}
				}
				var updated = await _clinicStaffRepository.UpdateEntity(existingClinicStaff);
				if (!updated)
				{
					_logger.LogError("Update ClinicStaff failed at repository level: Id {Id}", clinicStaff.Id);
					return false;
				}
				_logger.LogInformation("Update ClinicStaff success: Id {Id}", clinicStaff.Id);
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Update ClinicStaff exception: Id {Id}", clinicStaff.Id);
				return false;
			}
		}
	}
}
