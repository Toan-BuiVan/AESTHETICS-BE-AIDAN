using Aesthetics.Data.AestheticsInterfaces;
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

		public StaffService(ILogger<StaffService> logger
			, IStaffRepository staffRepository
			, IClinicStaffRepository clinicStaffRepository
			, IAccountRepository accountRepository
			, IClinicRepository clinicRepository)
		{
			_logger = logger;
			_staffRepository = staffRepository;
			_clinicStaffRepository = clinicStaffRepository;
			_accountRepository = accountRepository;
			_clinicRepository = clinicRepository;
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
				else if (searchRequest.ClinicId.HasValue)
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
					"GetList Staff success: Total {Total}, Returned {Returned}, IsDoctor: {IsDoctor}, ClinicId: {ClinicId}, ServiceTypeId: {ServiceTypeId}",
					totalCount, responseData.Count, searchRequest.IsDoctor, searchRequest.ClinicId, searchRequest.ServicetypeId);

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
	}
}