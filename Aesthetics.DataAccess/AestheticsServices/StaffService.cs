using Aesthetics.Data.AestheticsInterfaces;
using Aesthetics.Data.AestheticsInterfaces.ICommonService;
using Aesthetics.Data.RepositoryInterfaces;
using Aesthetics.Entities.Entities;
using Aesthetics.Entities.Models.RequestModel;
using Aesthetics.Entities.Models.ResponseModel;
using LinqKit;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using XAct;

namespace Aesthetics.Data.AestheticsServices
{
	public class StaffService : IStaffService
	{
		private readonly ILogger<StaffService> _logger;
		private readonly IStaffRepository _staffRepository;
		private readonly IClinicStaffRepository _clinicStaffRepository;
		private readonly IAccountRepository _accountRepository;
		private readonly IClinicRepository _clinicRepository;
		private readonly ICommonService _commonService;

		public StaffService(ILogger<StaffService> logger
			, IStaffRepository staffRepository
			, IClinicStaffRepository clinicStaffRepository
			, IAccountRepository accountRepository
			, IClinicRepository clinicRepository
			, ICommonService commonService)
		{
			_logger = logger;
			_staffRepository = staffRepository;
			_clinicStaffRepository = clinicStaffRepository;
			_accountRepository = accountRepository;
			_clinicRepository = clinicRepository;
			_commonService = commonService;
		}

		public async Task<BaseDataCollection<StaffResponseModel>> GetListAsync(RequestStaffSearch searchRequest)
		{
			try
			{
				Expression<Func<StaffEntity, bool>> predicate = x => x.DeleteStatus != true;

				//  Lọc theo IsDoctor
				if (searchRequest.IsDoctor.HasValue)
				{
					predicate = predicate.And(x => x.IsDoctor == searchRequest.IsDoctor.Value);
				}

				//  Load tất cả nhân viên khớp predicate
				var allMatching = await _staffRepository.FindByPredicate(predicate);
				var allMatchingList = allMatching.ToList();

				//  Nếu có ServiceTypeId, lọc nhân viên theo loại dịch vụ
				if (searchRequest.ServicetypeId.HasValue)
				{
					var clinicsWithServiceType = await _clinicRepository
						.FindByPredicate(x => x.ServiceTypeId == searchRequest.ServicetypeId.Value && !x.DeleteStatus);

					var clinicIds = clinicsWithServiceType.Select(c => c.Id).ToList();

					if (clinicIds.Any())
					{
						var staffInClinics = await _clinicStaffRepository
							.FindByPredicate(x => clinicIds.Contains(x.ClinicId ?? 0) && !x.DeleteStatus);

						var staffIds = staffInClinics.Select(x => x.StaffId).Distinct().ToList();
						allMatchingList = allMatchingList
							.Where(x => staffIds.Contains(x.Id))
							.ToList();
					}
					else
					{
						allMatchingList = new List<StaffEntity>();
					}
				}
				if (searchRequest.ClinicId.HasValue)
				{
					var staffInClinic = await _clinicStaffRepository
						.FindByPredicate(x => x.ClinicId == searchRequest.ClinicId.Value && !x.DeleteStatus);

					var staffIds = staffInClinic.Select(x => x.StaffId).ToList();
					allMatchingList = allMatchingList
						.Where(x => staffIds.Contains(x.Id))
						.ToList();
				}

				var totalCount = allMatchingList.Count;

				//  Batch load Accounts
				var accountIds = allMatchingList
					.Select(x => x.AccountId)
					.Distinct()
					.ToList();

				var accountsMap = new Dictionary<int, AccountEntity>();
				if (accountIds.Any())
				{
					var accounts = await _accountRepository.FindByPredicate(x =>
						accountIds.Contains(x.Id) && !x.DeleteStatus);

					accountsMap = accounts.ToDictionary(a => a.Id);
				}

				//  Batch load ClinicStaffs (để lấy danh sách ClinicIds của mỗi nhân viên)
				var staffIdsList = allMatchingList.Select(x => x.Id).ToList();
				var clinicStaffsMap = new Dictionary<int, List<ClinicStaffEntity>>();

				if (staffIdsList.Any())
				{
					var clinicStaffs = await _clinicStaffRepository
						.FindByPredicate(x => x.StaffId.HasValue && staffIdsList.Contains(x.StaffId.Value) && !x.DeleteStatus);

					clinicStaffsMap = clinicStaffs
						.GroupBy(x => x.StaffId.Value)
						.ToDictionary(g => g.Key, g => g.ToList());
				}

				// Phân trang
				var pagedData = allMatchingList
					.OrderBy(x => x.FullName ?? string.Empty)
					.Skip((searchRequest.PageNo - 1) * searchRequest.PageSize)
					.Take(searchRequest.PageSize)
					.ToList();

				// Map sang response model
				var responseData = pagedData.Select(staff => new StaffResponseModel
				{
					Id = staff.Id,
					AccountId = staff.AccountId,
					FullName = staff.FullName,
					DateBirth = staff.DateBirth,
					Sex = staff.Sex,
					Phone = staff.Phone,
					Address = staff.Address,
					IDCard = staff.IDCard,
					SalesPoints = staff.SalesPoints,
					StaffImage = staff.StaffImage,
					EmploymentStatus = staff.EmploymentStatus,
					IsDoctor = staff.IsDoctor,
					DoctorLevel = staff.DoctorLevel,
					Degree = staff.Degree,
					Email = staff.Email,
					Specialization = staff.Specialization,
					LicenseNumber = staff.LicenseNumber,
					ExperienceYears = staff.ExperienceYears,
					Biography = staff.Biography,
					AccountName = accountsMap.ContainsKey(staff.AccountId)
						? accountsMap[staff.AccountId].UserName
						: null,
					ClinicIds = clinicStaffsMap.ContainsKey(staff.Id)
						? clinicStaffsMap[staff.Id].Select(cs => cs.ClinicId ?? 0).ToList()
						: new List<int>()
				}).ToList();

				_logger.LogInformation(
					"GetList Staff success: Total {Total}, Returned {Returned}, IsDoctor: {IsDoctor}, ClinicId: {ClinicId}",
					totalCount, responseData.Count, searchRequest.IsDoctor, searchRequest.ClinicId);

				return new BaseDataCollection<StaffResponseModel>(
					responseData,
					totalCount,
					searchRequest.PageNo,
					searchRequest.PageSize);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GetList Staff exception. Request: {@Request}", searchRequest);
				return new BaseDataCollection<StaffResponseModel>(
					null,
					0,
					searchRequest?.PageNo ?? 1,
					searchRequest?.PageSize ?? 10);
			}
		}

		public async Task<bool> UpdateStaff(RequestUpdateStaff.UpdateStaffRequest request)
		{
			try
			{
				// ✅ Validate			
				if (request.AccountId <= 0)
				{
					_logger.LogWarning("UpdateStaff failed: Invalid AccountId {AccountId}", request.AccountId);
					return false;
				}

				_logger.LogInformation("UpdateStaff started: AccountId {AccountId}", request.AccountId);

				// ✅ Lấy staff theo AccountId
				var staffList = await _staffRepository.FindByPredicate(x => x.AccountId == request.AccountId && x.DeleteStatus != true);
				var staff = staffList.FirstOrDefault();

				if (staff == null)
				{
					_logger.LogWarning("UpdateStaff failed: Staff not found with AccountId {AccountId}", request.AccountId);
					return false;
				}

				_logger.LogInformation("UpdateStaff: Found staff {StaffId} - Current FullName: {FullName}",
					staff.Id, staff.FullName);

				// ✅ Update thông tin
				var updatedFields = new List<string>();

				if (!string.IsNullOrWhiteSpace(request.FullName) && staff.FullName != request.FullName)
				{
					_logger.LogInformation("UpdateStaff: FullName changed from '{OldValue}' to '{NewValue}'",
						staff.FullName, request.FullName);
					staff.FullName = request.FullName;
					updatedFields.Add("FullName");
				}

				if (request.EmploymentStatus.HasValue && staff.EmploymentStatus != request.EmploymentStatus)
				{
					_logger.LogInformation("UpdateStaff: EmploymentStatus changed from '{OldValue}' to '{NewValue}'",
						staff.EmploymentStatus, request.EmploymentStatus);
					staff.EmploymentStatus = request.EmploymentStatus;
					updatedFields.Add("EmploymentStatus");
				}

				if (request.DateBirth.HasValue && staff.DateBirth != request.DateBirth)
				{
					_logger.LogInformation("UpdateStaff: DateBirth changed from '{OldValue}' to '{NewValue}'",
						staff.DateBirth, request.DateBirth);
					staff.DateBirth = request.DateBirth;
					updatedFields.Add("DateBirth");
				}

				if (!string.IsNullOrWhiteSpace(request.Sex) && staff.Sex != request.Sex)
				{
					_logger.LogInformation("UpdateStaff: Sex changed from '{OldValue}' to '{NewValue}'",
						staff.Sex, request.Sex);
					staff.Sex = request.Sex;
					updatedFields.Add("Sex");
				}

				if (!string.IsNullOrWhiteSpace(request.Phone) && staff.Phone != request.Phone )
				{
					_logger.LogInformation("UpdateStaff: Phone changed from '{OldValue}' to '{NewValue}'",
						staff.Phone, request.Phone);
					staff.Phone = request.Phone;
					updatedFields.Add("Phone");
				}

				if (!string.IsNullOrWhiteSpace(request.Address) && staff.Address != request.Address)
				{
					_logger.LogInformation("UpdateStaff: Address changed from '{OldValue}' to '{NewValue}'",
						staff.Address, request.Address);
					staff.Address = request.Address;
					updatedFields.Add("Address");
				}

				if (!string.IsNullOrWhiteSpace(request.IDCard) && staff.IDCard != request.IDCard)
				{
					_logger.LogInformation("UpdateStaff: IDCard changed from '{OldValue}' to '{NewValue}'",
						staff.IDCard, request.IDCard);
					staff.IDCard = request.IDCard;
					updatedFields.Add("IDCard");
				}

				if (!string.IsNullOrWhiteSpace(request.StaffImage))
				{
					var processedImages = await _commonService.BaseProcessingFunction64(request.StaffImage);
					staff.StaffImage = processedImages;
					updatedFields.Add("StaffImage");
				}

				if (request.IsDoctor.HasValue && staff.IsDoctor != request.IsDoctor)
				{
					_logger.LogInformation("UpdateStaff: IsDoctor changed from '{OldValue}' to '{NewValue}'",
						staff.IsDoctor, request.IsDoctor);
					staff.IsDoctor = request.IsDoctor;
					updatedFields.Add("IsDoctor");
				}

				if (request.DoctorLevel.HasValue && staff.DoctorLevel != request.DoctorLevel)
				{
					_logger.LogInformation("UpdateStaff: DoctorLevel changed from '{OldValue}' to '{NewValue}'",
						staff.DoctorLevel, request.DoctorLevel);
					staff.DoctorLevel = request.DoctorLevel;
					updatedFields.Add("DoctorLevel");
				}

				if (!string.IsNullOrWhiteSpace(request.Degree) && staff.Degree != request.Degree)
				{
					_logger.LogInformation("UpdateStaff: Degree changed from '{OldValue}' to '{NewValue}'",
						staff.Degree, request.Degree);
					staff.Degree = request.Degree;
					updatedFields.Add("Degree");
				}

				if (!string.IsNullOrWhiteSpace(request.Specialization) && staff.Specialization != request.Specialization)
				{
					_logger.LogInformation("UpdateStaff: Specialization changed from '{OldValue}' to '{NewValue}'",
						staff.Specialization, request.Specialization);
					staff.Specialization = request.Specialization;
					updatedFields.Add("Specialization");
				}

				if (!string.IsNullOrWhiteSpace(request.LicenseNumber) && staff.LicenseNumber != request.LicenseNumber)
				{
					_logger.LogInformation("UpdateStaff: LicenseNumber changed from '{OldValue}' to '{NewValue}'",
						staff.LicenseNumber, request.LicenseNumber);
					staff.LicenseNumber = request.LicenseNumber;
					updatedFields.Add("LicenseNumber");
				}

				if (request.ExperienceYears.HasValue && staff.ExperienceYears != request.ExperienceYears)
				{
					_logger.LogInformation("UpdateStaff: ExperienceYears changed from '{OldValue}' to '{NewValue}'",
						staff.ExperienceYears, request.ExperienceYears);
					staff.ExperienceYears = request.ExperienceYears;
					updatedFields.Add("ExperienceYears");
				}

				if (!string.IsNullOrWhiteSpace(request.Biography) && staff.Biography != request.Biography)
				{
					_logger.LogInformation("UpdateStaff: Biography updated");
					staff.Biography = request.Biography;
					updatedFields.Add("Biography");
				}

				// ✅ Log tất cả fields đã thay đổi
				if (updatedFields.Count == 0)
				{
					_logger.LogInformation("UpdateStaff: No fields changed for AccountId {AccountId}", request.AccountId);
					return false;
				}

				_logger.LogInformation("UpdateStaff: Fields changed - {ChangedFields}", string.Join(", ", updatedFields));

				// ✅ Lưu vào database
				_logger.LogInformation("UpdateStaff: Saving to database for AccountId {AccountId}", request.AccountId);
				var success = await _staffRepository.UpdateEntity(staff);

				if (!success)
				{
					_logger.LogError("UpdateStaff failed at repository level: AccountId {AccountId}", request.AccountId);
					return false;
				}

				_logger.LogInformation("UpdateStaff completed successfully: AccountId {AccountId}, Updated fields: {FieldCount}",
					request.AccountId, updatedFields.Count);

				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "UpdateStaff exception for AccountId {AccountId}. Exception Message: {Message}",
					request.AccountId, ex.Message);
				return false;
			}
		}
	}
}