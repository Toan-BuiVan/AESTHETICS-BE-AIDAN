using Aesthetics.Data.AestheticsInterfaces;
using Aesthetics.Data.RepositoryInterfaces;
using Aesthetics.Entities.Entities;
using Aesthetics.Entities.Models.RequestModel;
using Aesthetics.Entities.Models.ResponseModel;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XAct;

namespace Aesthetics.Data.AestheticsServices
{
	public class StaffShiftServices : IStaffShiftServices
	{
		private readonly ILogger<ClinicService> _logger;
		private readonly IStaffShiftRepository _staffShiftRepository;
		private readonly IStaffRepository _staffRepository;
		private readonly IClinicStaffRepository _clinicStaffRepository;

		public StaffShiftServices(ILogger<ClinicService> logger
			, IStaffShiftRepository staffShiftRepository
			, IStaffRepository staffRepository
			, IClinicStaffRepository clinicStaffRepository)
		{
			_logger = logger;
			_staffShiftRepository = staffShiftRepository;
			_staffRepository = staffRepository;
			_clinicStaffRepository = clinicStaffRepository;
		}

		private async Task<bool> ValidationStaffShift(CreateStaffShift staffShift)
		{
			if (staffShift?.StaffId == null || staffShift?.Date == null)
			{
				_logger.LogWarning("VALIDATE_STAFF_SHIFT_NULL: StaffId or Date is null");
				return false;
			}

			var targetDate = staffShift.Date.Value.Date;

			var staff = await _staffRepository.GetById(staffShift.StaffId.Value);
			if (staff == null)
			{
				_logger.LogWarning("VALIDATE_STAFF_SHIFT_NOT_FOUND: StaffId {StaffId} not found", staffShift.StaffId);
				return false;
			}

			bool isDoctor = staff.IsDoctor ?? false;

			var clinicStaffs = await _clinicStaffRepository.FindByPredicate(x => x.StaffId == staffShift.StaffId.Value && !x.DeleteStatus);
			if (!clinicStaffs.Any())
			{
				_logger.LogWarning("VALIDATE_STAFF_SHIFT_NO_CLINIC: StaffId {StaffId} not assigned to any clinic", staffShift.StaffId);
				return false;
			}

			if (clinicStaffs.Count > 1)
			{
				_logger.LogWarning("VALIDATE_STAFF_SHIFT_MULTIPLE_CLINICS: StaffId {StaffId} assigned to multiple clinics", staffShift.StaffId);
				return false;
			}

			var clinicId = clinicStaffs.First().ClinicId;

			var existingForStaff = await _staffShiftRepository.FindByPredicate(x =>
				x.StaffId == staffShift.StaffId.Value &&
				x.Date.Value.Date == targetDate &&
				!x.DeleteStatus);

			if (existingForStaff.Any())
			{
				_logger.LogWarning("VALIDATE_STAFF_SHIFT_DUPLICATE: StaffId {StaffId} already has a shift on {Date:yyyy-MM-dd}",
					staffShift.StaffId, targetDate);
				return false;
			}

			_logger.LogInformation("VALIDATE_STAFF_SHIFT_SUCCESS: StaffId {StaffId} passed validation", staffShift.StaffId);
			return true;
		}

		public async Task<bool> create(CreateStaffShift staffShift)
		{
			try
			{
				if (staffShift == null)
				{
					_logger.LogWarning("Create StaffShift failed: staffShift is null");
					return false;
				}
				//if (!await ValidationStaffShift(staffShift))
				//	return false;

				var entity = new StaffShiftEntity
				{
					StaffId = staffShift.StaffId.Value,
					Date = staffShift.Date,
					StartDate = staffShift.StartDate,
					EndDate = staffShift.EndDate,
					Status = 2,
					DeleteStatus = false
				};

				var created = await _staffShiftRepository.CreateEntity(entity);

				if (!created)
				{
					_logger.LogError("Create StaffShift failed at repository level: StaffId {StaffId}",
						staffShift.StaffId);
					return false;
				}

				_logger.LogInformation("Create StaffShift success: StaffId {StaffId}",
					staffShift.StaffId);

				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Create StaffShift exception: StaffId {StaffId}", staffShift.StaffId);
				return false;
			}
		}

		public async Task<bool> delete(DeleteStaffShift staffShift)
		{
			try
			{

				var entity = await _staffShiftRepository.GetById(staffShift.Id);

				if (entity == null || entity.DeleteStatus)
					return false;

				var deleted = await _staffShiftRepository.DeleteEntitiesStatus(entity);

				if (!deleted)
				{
					_logger.LogError("Delete StaffShift failed Id {Id}", staffShift.Id);
					return false;
				}

				_logger.LogInformation("Delete StaffShift success Id {Id}", staffShift.Id);
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Delete StaffShift exception Id {Id}", staffShift.Id);
				return false;
			}
		}

		public async Task<BaseDataCollection<StaffShiftEntity>> getlist(GetStaffShift request)
		{
			try
			{
				_logger.LogInformation("GET_LIST_STAFF_SHIFT_START: PageNo {PageNo}, PageSize {PageSize}",
					request?.PageNo, request?.PageSize);

				var query = (await _staffShiftRepository
					.FindByPredicate(x => !x.DeleteStatus))
					.ToList();

				// ✅ Filter by StaffId
				if (request?.StaffId.HasValue == true)
				{
					query = query.Where(x => x.StaffId == request.StaffId.Value).ToList();
					_logger.LogInformation("GET_LIST_STAFF_SHIFT_FILTER_STAFF: StaffId {StaffId}, Found {Count} shifts",
						request.StaffId, query.Count);
				}

				// ✅ Filter by Date
				if (request?.Date.HasValue == true)
				{
					var targetDate = request.Date.Value.Date;
					query = query.Where(x => x.Date.HasValue && x.Date.Value.Date == targetDate).ToList();
					_logger.LogInformation("GET_LIST_STAFF_SHIFT_FILTER_DATE: Date {Date:yyyy-MM-dd}, Found {Count} shifts",
						targetDate, query.Count);
				}

				// ✅ UPDATED: Removed ShiftType filter since it's no longer part of the entity
				if (request?.Id.HasValue == true)
				{
					query = query.Where(x => x.Id == request.Id.Value).ToList();
					_logger.LogInformation("GET_LIST_STAFF_SHIFT_FILTER_ID: Id {Id}, Found {Count} shifts",
						request.Id, query.Count);
				}

				int total = query.Count;

				// ✅ Sorting: newest first (by Date descending, then by StartDate descending)
				query = query
					.OrderByDescending(x => x.Date)
					.ThenByDescending(x => x.StartDate)
					.ToList();

				// ✅ Pagination
				if (request?.PageSize > 0)
				{
					query = query
						.Skip((request.PageNo - 1) * request.PageSize)
						.Take(request.PageSize)
						.ToList();

					_logger.LogInformation("GET_LIST_STAFF_SHIFT_PAGINATION: PageNo {PageNo}, PageSize {PageSize}, Returned {Count}",
						request.PageNo, request.PageSize, query.Count);
				}

				_logger.LogInformation("GET_LIST_STAFF_SHIFT_SUCCESS: Total {Total} shifts",
					total);

				return new BaseDataCollection<StaffShiftEntity>
				{
					TotalRecordCount = total,
					BaseDatas = query,
					PageIndex = request?.PageNo ?? 1,
					PageCount = request?.PageSize > 0 ? (int)Math.Ceiling((double)total / request.PageSize) : 1
				};
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GET_LIST_STAFF_SHIFT_EXCEPTION");
				return new BaseDataCollection<StaffShiftEntity>();
			}
		}
	}
}
