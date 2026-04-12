using Aesthetics.Data.AestheticsInterfaces;
using Aesthetics.Data.RepositoryInterfaces;
using Aesthetics.Entities.Models.RequestModel;
using Aesthetics.Entities.Models.ResponseModel;
using Aesthetics.Entities.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Data.AestheticsServices
{ 
	public class CustomerService : ICustomerService
	{
		private readonly ICustomerRepository _customerRepository;
		private readonly ILogger<CustomerService> _logger;

		public CustomerService(ICustomerRepository customerRepository, ILogger<CustomerService> logger)
		{
			_customerRepository = customerRepository;
			_logger = logger;
		}

		public async Task<bool> UpdateCustomer(RequestUpdateCustomer request)
		{
			try
			{
				// ✅ Validate
				if (request.AccountId <= 0)
				{
					_logger.LogWarning("UpdateCustomer failed: Invalid AccountId {AccountId}", request.AccountId);
					return false;
				}

				_logger.LogInformation("UpdateCustomer started: AccountId {AccountId}", request.AccountId);

				// ✅ Lấy customer theo AccountId
				var customers = await _customerRepository.FindByPredicate(x => x.AccountId == request.AccountId && x.DeleteStatus != true);
				var customer = customers.FirstOrDefault();

				if (customer == null)
				{
					_logger.LogWarning("UpdateCustomer failed: Customer not found with AccountId {AccountId}", request.AccountId);
					return false;
				}

				// ✅ Update thông tin
				if (!string.IsNullOrWhiteSpace(request.FullName))
					customer.FullName = request.FullName;

				if (request.DateBirth.HasValue)
					customer.DateBirth = request.DateBirth;

				if (!string.IsNullOrWhiteSpace(request.Sex))
					customer.Sex = request.Sex;

				if (!string.IsNullOrWhiteSpace(request.Phone))
					customer.Phone = request.Phone;

				if (!string.IsNullOrWhiteSpace(request.Address))
					customer.Address = request.Address;

				if (!string.IsNullOrWhiteSpace(request.Email))
					customer.Email = request.Email;

				if (!string.IsNullOrWhiteSpace(request.IDCard))
					customer.IDCard = request.IDCard;

				// ✅ Lưu vào database
				_logger.LogInformation("UpdateCustomer: Saving to database for AccountId {AccountId}", request.AccountId);
				var success = await _customerRepository.UpdateEntity(customer);

				if (!success)
				{
					_logger.LogError("UpdateCustomer failed at repository level: AccountId {AccountId}", request.AccountId);
					return false;
				}

				_logger.LogInformation("UpdateCustomer completed successfully: AccountId {AccountId}",
					request.AccountId);

				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "UpdateCustomer exception for AccountId {AccountId}. Exception Message: {Message}",
					request.AccountId, ex.Message);
				return false;
			}
		}

		public async Task<BaseDataCollection<CustomerEntity>> GetListCustomer(RequestCustomer request)
		{
			try
			{
				// ✅ Validate
				if (request.PageNo <= 0)
					request.PageNo = 1;
				if (request.PageSize <= 0)
					request.PageSize = 10;

				_logger.LogInformation("GetListCustomer started: PageNo {PageNo}, PageSize {PageSize}", request.PageNo, request.PageSize);

				// ✅ Build predicate for filtering
				Expression<Func<CustomerEntity, bool>> predicate = x => x.DeleteStatus != true;

				if (request.Id.HasValue)
				{
					predicate = x => x.Id == request.Id.Value && x.DeleteStatus != true;
				}

				if (!string.IsNullOrWhiteSpace(request.FullName))
				{
					var fullName = request.FullName.ToLower();
					predicate = x => x.FullName.ToLower().Contains(fullName) && x.DeleteStatus != true;
				}

				// ✅ Get all matching customers
				var allMatching = await _customerRepository.FindByPredicate(predicate);
				var totalCount = allMatching.Count;

				// ✅ Apply pagination and sorting (newest first)
				var pagedData = allMatching
					.OrderByDescending(x => x.Id)
					.Skip((request.PageNo - 1) * request.PageSize)
					.Take(request.PageSize)
					.ToList();

				_logger.LogInformation("GetListCustomer completed: Found {Count} records, returning {PageSize} for page {PageNo}",
					totalCount, pagedData.Count, request.PageNo);

				return new BaseDataCollection<CustomerEntity>(
					pagedData,
					totalCount,
					request.PageNo,
					request.PageSize
				);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GetListCustomer exception");
				return new BaseDataCollection<CustomerEntity>(
					null,
					0,
					request.PageNo,
					request.PageSize
				);
			}
		}
	}
}
