using Aesthetics.Data.AestheticsInterfaces;
using Aesthetics.Data.AestheticsInterfaces.TokenService;
using Aesthetics.Data.RepositoryInterfaces;
using Aesthetics.Data.RepositoryServices;
using Aesthetics.Entities.Entities;
using Aesthetics.Entities.Enum;
using Aesthetics.Entities.Models.RequestModel;
using Aesthetics.Entities.Models.ResponseModel;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Data.AestheticsServices
{
    public class AccountService : IAccountService
	{
		private readonly ILogger<AccountService> _logger;
		private readonly IAccountRepository _accountRepository;
		private IHttpContextAccessor _httpContextAccessor;
		private ITokenService _tokenService;
		private ICustomerRepository _customerRepository;
		private IStaffRepository _staffRepository;
		private ICartRepository _cartRepository;

		public AccountService(ILogger<AccountService> logger
			, IAccountRepository accountRepository
			, IHttpContextAccessor httpContextAccessor
			, ITokenService tokenService
			, ICustomerRepository customerRepository
			, IStaffRepository staffRepository
			, ICartRepository cartRepository)
		{
			_logger = logger;
			_accountRepository = accountRepository;
			_httpContextAccessor = httpContextAccessor;
			_tokenService = tokenService;
			_customerRepository = customerRepository;
			_staffRepository = staffRepository;
			_cartRepository = cartRepository;
		}

		public async Task<bool> create(RequestAccount request)
		{
			try
			{
				_logger.LogInformation("Start Create Account");
				if (string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.PassWord))
				{
					_logger.LogWarning("Invalid username or password provided.");
					return false;
				}
				var account = new AccountEntity
				{
					UserName = request.UserName,
					PassWord = Security.EncryptPassWord(request.PassWord), 
					Creation = DateTime.Now,
					DeleteStatus = false,
					Role = (int)request.AccountType,
				};
				await _accountRepository.CreateEntity(account);

				switch (request.AccountType)
				{
					case AccountRole.Customer: 
						var customer = new CustomerEntity
						{
							AccountId = account.Id,
							ReferralCode = await _accountRepository.GenerateUniqueReferralCode(),
							AccumulatedPoints = 0,
							RatingPoints = 0,
							RankMember = "Bronze",
							DeleteStatus = false
						};
						await _customerRepository.CreateEntity(customer);

						var cart = new CartEntity
						{
							CustomerId = customer.Id,
							CreationDate = DateTime.Now,
							DeleteStatus = false
						};
						await _cartRepository.CreateEntity(cart);

						var referrerUser = await _customerRepository.GetUserIdByReferralCode(request.ReferralCode);
						if (referrerUser != null)
						{
							await _customerRepository.UpdateAccumulatedPoints(referrerUser.Id);
						}

						/*
						 * Thêm quyền cho khách hàng
						 */
						break;

					case AccountRole.Staff:
						var staff = new StaffEntity
						{
							AccountId = account.Id,
							SalesPoints = 0,
							EmploymentStatus = (int)EmploymentStatus.Active,
							DeleteStatus = false,
							IsDoctor = request.IsDoctor ?? false,
							LicenseNumber = null
						};
						await _staffRepository.CreateEntity(staff);

						/*
						 * Thêm quyền cho nhân viên
						 */
						
						break;

					default:
						throw new ArgumentException("Invalid AccountType");
				}

				_logger.LogInformation("Account created successfully");
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error creating account");
				return false;
			}
		}

		public async Task<bool> delete(DeleteAccount account)
		{
			try
			{
				_logger.LogInformation("Start deleting Account");

				var existingAccount = await _accountRepository.GetById(account.Id);
				if (existingAccount == null)
				{
					_logger.LogWarning("Delete Account failed: Not found with Id {Id}", account.Id);
					return false;
				}

				var deleted = await _accountRepository.DeleteRangeEntitiesStatus(existingAccount);
				if (!deleted)
				{
					_logger.LogError("Delete Account failed at repository level: Id {Id}", account.Id);
					return false;
				}

				_logger.LogInformation("Delete Account success: Id {Id}", account.Id);
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Delete Account exception: Id {Id}", account.Id);
				return false;
			}
		}

		public async Task<BaseDataCollection<AccountEntity>> getlist(AccountGet account)
		{
			try
			{
				Expression<Func<AccountEntity, bool>> predicate = x => x.DeleteStatus != true;

				if (account.Id.HasValue)
				{
					predicate = x => x.Id == account.Id.Value && x.DeleteStatus != true;
				}

				if (!string.IsNullOrWhiteSpace(account.UserName))
				{
					var username = account.UserName.ToLower();
					predicate = x =>
						x.UserName.ToLower().Contains(username)
						&& x.DeleteStatus != true;
				}

				var allMatching = await _accountRepository.FindByPredicate(predicate);
				var totalCount = allMatching.Count;

				var pagedData = allMatching
					.OrderBy(x => x.UserName)
					.Skip((account.PageNo - 1) * account.PageSize)
					.Take(account.PageSize)
					.ToList();

				return new BaseDataCollection<AccountEntity>(
					pagedData,
					totalCount,
					account.PageNo,
					account.PageSize
				);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GetList Account exception");
				return new BaseDataCollection<AccountEntity>(
					null,
					0,
					account.PageNo,
					account.PageSize
				);
			}
		}

		public async Task<bool> update(UpdateAccount account)
		{
			try
			{
				_logger.LogInformation("Start updating Account password");

				var existingAccount = await _accountRepository.GetById(account.Id);
				if (existingAccount == null)
				{
					_logger.LogWarning("Update Account failed: Not found with Id {Id}", account.Id);
					return false;
				}

				if (string.IsNullOrWhiteSpace(account.PassWord))
				{
					_logger.LogWarning("Update Account failed: Password is empty. Id {Id}", account.Id);
					return false;
				}

				existingAccount.PassWord = account.PassWord;

				var updated = await _accountRepository.UpdateEntity(existingAccount);
				if (!updated)
				{
					_logger.LogError("Update Account failed at repository level: Id {Id}", account.Id);
					return false;
				}

				_logger.LogInformation("Update Account password success: Id {Id}", account.Id);
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Update Account password exception: Id {Id}", account.Id);
				return false;
			}
		}

		public async Task<AccountProfileResponseModel?> GetProfileByAccountIdAsync(int accountId)
		{
			try
			{
				// Lấy thông tin Account trước
				var accountList = await _accountRepository.FindByPredicate(
					x => x.Id == accountId && x.DeleteStatus != true);

				var account = accountList.FirstOrDefault();
				if (account == null)
					return null;

				// Ưu tiên lấy KHÁCH HÀNG
				var customerList = await _customerRepository.FindByPredicate(
					x => x.AccountId == accountId && x.DeleteStatus != true);

				if (customerList.Any())
				{
					var cust = customerList.First();

					return new AccountProfileResponseModel
					{
						AccountId = account.Id,
						UserName = account.UserName ?? string.Empty,
						CreationDate = account.Creation ?? DateTime.MinValue,
						IsDeleted = account.DeleteStatus,
						IsCustomer = true,

						FullName = cust.FullName,
						Phone = cust.Phone,
						Address = cust.Address,
						Email = cust.Email,
						IDCard = cust.IDCard,
						DateBirth = cust.DateBirth,
						Sex = cust.Sex,
						ReferralCode = cust.ReferralCode,
						AccumulatedPoints = cust.AccumulatedPoints,
						RatingPoints = cust.RatingPoints,
						RankMember = cust.RankMember
					};
				}

				// Nếu không có khách hàng → lấy NHÂN VIÊN
				var staffList = await _staffRepository.FindByPredicate(
					x => x.AccountId == accountId && x.DeleteStatus != true);

				if (staffList.Any())
				{
					var staff = staffList.First();

					return new AccountProfileResponseModel
					{
						AccountId = account.Id,
						UserName = account.UserName ?? string.Empty,
						CreationDate = account.Creation ?? DateTime.MinValue,
						IsDeleted = account.DeleteStatus,
						IsCustomer = false,

						FullName = staff.FullName,
						Phone = staff.Phone,
						Address = staff.Address,
						IDCard = staff.IDCard,

						IsDoctor = staff.IsDoctor ?? false,
						StaffImage = staff.StaffImage,
						EmploymentStatus = staff.EmploymentStatus,
						DoctorLevel = staff.DoctorLevel,
						Degree = staff.Degree,
						Specialization = staff.Specialization,
						LicenseNumber = staff.LicenseNumber,
						ExperienceYears = staff.ExperienceYears,
						Biography = staff.Biography,
						SalesPoints = staff.SalesPoints
					};
				}

				return null; 
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GetProfileByAccountIdAsync failed for AccountId: {AccountId}", accountId);
				return null;
			}
		}
	}
}
