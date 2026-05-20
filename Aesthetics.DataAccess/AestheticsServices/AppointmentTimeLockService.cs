using Aesthetics.Data.AestheticsInterfaces;
using Aesthetics.Data.RepositoryInterfaces;
using Aesthetics.Data.RepositoryServices;
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
    public class AppointmentTimeLockService : IAppointmentTimeLockService
	{
		private readonly ILogger<AppointmentTimeLockService> _logger;
		private readonly IAppointmentTimeLockRepository _appointmentTimeLockRepository;
		private readonly IClinicStaffRepository _clinicStaffRepository;
		public AppointmentTimeLockService(ILogger<AppointmentTimeLockService> logger
			, IAppointmentTimeLockRepository appointmentTimeLockRepository
			, IClinicStaffRepository clinicStaffRepository)
		{
			_logger = logger;
			_appointmentTimeLockRepository = appointmentTimeLockRepository;
			_clinicStaffRepository = clinicStaffRepository;
		}

		public async Task<bool> create(CreateAppointmentTimeLock timeLock)
		{
			try
			{
				if (!timeLock.StaffId.HasValue || !timeLock.StartTime.HasValue || !timeLock.EndTime.HasValue)
				{
					_logger.LogWarning("Create AppointmentTimeLock failed: Missing required fields");
					return false;
				}

				var clinicStaff = await _clinicStaffRepository
					.FindByPredicate(x => x.StaffId == timeLock.StaffId.Value && !x.DeleteStatus);

				var clinic = clinicStaff.FirstOrDefault();
				if (clinic == null)
				{
					_logger.LogWarning("Create AppointmentTimeLock failed: Staff not assigned to clinic. StaffId {StaffId}", timeLock.StaffId);
					return false;
				}

				var entity = new AppointmentTimeLockEntity
				{
					ClinicId = clinic.ClinicId,
					StartTime = timeLock.StartTime,
					EndTime = timeLock.EndTime,
					IsOverloaded = timeLock.IsOverloaded ?? false,
					DeleteStatus = false,
					CreationDate = DateTime.Now,
					CreatedBy = timeLock.StaffId
				};

				var created = await _appointmentTimeLockRepository.CreateEntity(entity);
				if (!created)
				{
					_logger.LogError("Create AppointmentTimeLock failed at repository level");
					return false;
				}

				_logger.LogInformation("Create AppointmentTimeLock success: StaffId {StaffId}", timeLock.StaffId);
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Create AppointmentTimeLock exception");
				return false;
			}
		}

		public async Task<bool> delete(DeleteAppointmentTimeLock timeLock)
		{
			try
			{
				if (!timeLock.Id.HasValue)
				{
					_logger.LogWarning("Delete AppointmentTimeLock failed: Missing Id");
					return false;
				}

				var existing = await _appointmentTimeLockRepository.GetById(timeLock.Id.Value);
				if (existing == null)
				{
					_logger.LogWarning("Delete AppointmentTimeLock failed: Not found with Id {Id}", timeLock.Id);
					return false;
				}

				var deleted = await _appointmentTimeLockRepository.DeleteEntitiesStatus(existing);
				if (!deleted)
				{
					_logger.LogError("Delete AppointmentTimeLock failed at repository level: Id {Id}", timeLock.Id);
					return false;
				}

				_logger.LogInformation("Delete AppointmentTimeLock success: Id {Id}", timeLock.Id);
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Delete AppointmentTimeLock exception: Id {Id}", timeLock.Id);
				return false;
			}
		}

		public async Task<BaseDataCollection<AppointmentTimeLockEntity>> getlist(GetAppointmentTimeLock timeLock)
		{
			try
			{
				Expression<Func<AppointmentTimeLockEntity, bool>> predicate = x => !x.DeleteStatus;

				if (timeLock.ClinicId.HasValue)
				{
					predicate = x => x.ClinicId == timeLock.ClinicId && !x.DeleteStatus;
				}

				if (timeLock.IsOverloaded.HasValue)
				{
					predicate = x => x.IsOverloaded == timeLock.IsOverloaded && !x.DeleteStatus;
				}

				if (timeLock.StartTime.HasValue)
				{
					predicate = x => x.StartTime >= timeLock.StartTime && !x.DeleteStatus;
				}

				if (timeLock.EndTime.HasValue)
				{
					predicate = x => x.EndTime <= timeLock.EndTime && !x.DeleteStatus;
				}

				var allMatching = await _appointmentTimeLockRepository.FindByPredicate(predicate);
				var totalCount = allMatching.Count;

				var pagedData = allMatching
					.OrderBy(x => x.StartTime)
					.Skip((timeLock.PageNo - 1) * timeLock.PageSize)
					.Take(timeLock.PageSize)
					.ToList();

				return new BaseDataCollection<AppointmentTimeLockEntity>(
					pagedData,
					totalCount,
					timeLock.PageNo,
					timeLock.PageSize
				);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GetList AppointmentTimeLock exception");

				return new BaseDataCollection<AppointmentTimeLockEntity>(
					null,
					0,
					timeLock.PageNo,
					timeLock.PageSize
				);
			}
		}

		public async Task<bool> update(UpdateAppointmentTimeLock timeLock)
		{
			try
			{
				if (!timeLock.Id.HasValue)
				{
					_logger.LogWarning("Update AppointmentTimeLock failed: Missing Id");
					return false;
				}

				var existing = await _appointmentTimeLockRepository.GetById(timeLock.Id.Value);
				if (existing == null)
				{
					_logger.LogWarning("Update AppointmentTimeLock failed: Not found with Id {Id}", timeLock.Id);
					return false;
				}

				if (timeLock.StartTime.HasValue)
					existing.StartTime = timeLock.StartTime;

				if (timeLock.EndTime.HasValue)
					existing.EndTime = timeLock.EndTime;

				if (timeLock.IsOverloaded.HasValue)
					existing.IsOverloaded = timeLock.IsOverloaded;

				var updated = await _appointmentTimeLockRepository.UpdateEntity(existing);
				if (!updated)
				{
					_logger.LogError("Update AppointmentTimeLock failed at repository level: Id {Id}", timeLock.Id);
					return false;
				}

				_logger.LogInformation("Update AppointmentTimeLock success: Id {Id}", timeLock.Id);
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Update AppointmentTimeLock exception: Id {Id}", timeLock.Id);
				return false;
			}
		}
	}
}
