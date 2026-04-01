using Aesthetics.Data.AestheticsInterfaces;
using Aesthetics.Data.RepositoryInterfaces;
using Aesthetics.Entities.Models.RequestModel;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
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
	}
}
