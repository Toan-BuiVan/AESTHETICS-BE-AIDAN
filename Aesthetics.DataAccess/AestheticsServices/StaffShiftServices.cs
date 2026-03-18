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
			var targetDate = staffShift.Date.Value.Date;
			var targetShiftType = staffShift.ShiftType.Value;

			var staff = await _staffRepository.GetById(staffShift.StaffId.Value);
			if (staff == null)
			{
				return false;
			}

			bool isDoctor = staff.IsDoctor ?? false;

			var clinicStaffs = await _clinicStaffRepository.FindByPredicate(x => x.StaffId == staffShift.StaffId.Value && !x.DeleteStatus);
			if (!clinicStaffs.Any())
			{
				return false;
			}
			if (clinicStaffs.Count > 1)
			{
				return false;
			}
			var clinicId = clinicStaffs.First().ClinicId;

			// Kiểm tra duplicate cho Staff này trong ca
			var existingForStaff = await _staffShiftRepository.FindByPredicate(x =>
				x.StaffId == staffShift.StaffId.Value &&
				x.Date.Value.Date == targetDate &&
				x.ShiftType == targetShiftType &&
				!x.DeleteStatus);
			if (existingForStaff.Any())
			{
				return false;
			}

			// Lấy tất cả StaffId cùng Clinic và cùng loại (bác sĩ/nhân viên)
			var staffIdsInClinic = (await _clinicStaffRepository.FindByPredicate(x => x.ClinicId == clinicId && !x.DeleteStatus))
				.Select(x => x.StaffId).ToList();

			var staffList = await _staffRepository.FindByPredicate(x => staffIdsInClinic.Contains(x.Id) && !x.DeleteStatus);

			var filteredStaffIds = staffList
				.Where(x => (isDoctor ? (x.IsDoctor ?? false) : !(x.IsDoctor ?? false)))
				.Select(x => x.Id)
				.ToList();

			// Count số đăng ký trong ca đó của Clinic cho loại này
			var shiftsInCa = await _staffShiftRepository.FindByPredicate(x =>
				x.Date.Value.Date == targetDate &&
				x.ShiftType == targetShiftType &&
				filteredStaffIds.Contains(x.StaffId ?? 0) &&
				!x.DeleteStatus);

			int maxLimit = isDoctor ? 5 : 8;
			if (shiftsInCa.Count >= maxLimit)
			{
				return false;
			}
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
				if (!await ValidationStaffShift(staffShift))
					return false;

				var entity = new StaffShiftEntity
				{
					StaffId = staffShift.StaffId.Value,
					Date = staffShift.Date,
					ShiftType = staffShift.ShiftType,
					Status = false,
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
				var query = (await _staffShiftRepository
							.FindByPredicate(x => !x.DeleteStatus))
							.ToList();

				if (request.StaffId.HasValue)
					query = query.Where(x => x.StaffId == request.StaffId.Value).ToList();

				if (request.Date.HasValue)
				{
					var targetDate = request.Date.Value.Date;
					query = query.Where(x => x.Date.Value.Date == targetDate).ToList();
				}

				if (request.ShiftType.HasValue)
					query = query.Where(x => x.ShiftType == request.ShiftType.Value).ToList();

				int total = query.Count;

				if (request.PageSize > 0)
				{
					query = query
						.Skip((request.PageNo - 1) * request.PageSize)
						.Take(request.PageSize)
						.ToList();
				}

				return new BaseDataCollection<StaffShiftEntity>
				{
					TotalRecordCount = total,
					BaseDatas = query
				};
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GetList StaffShift exception");
				return new BaseDataCollection<StaffShiftEntity>();
			}
		}
	}
}
