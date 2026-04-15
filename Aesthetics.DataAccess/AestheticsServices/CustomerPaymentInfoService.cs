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
	public class CustomerPaymentInfoService : ICustomerPaymentInfoService
	{

		private readonly ILogger<CustomerPaymentInfoService> _logger;
		private readonly ICustomerPaymentInfoRepository _customerPaymentInfoRepository;
		private readonly ICustomerRepository _customerRepository;

		public CustomerPaymentInfoService(
			ILogger<CustomerPaymentInfoService> logger,
			ICustomerPaymentInfoRepository customerPaymentInfoRepository,
			ICustomerRepository customerRepository)
		{
			_logger = logger;
			_customerPaymentInfoRepository = customerPaymentInfoRepository;
			_customerRepository = customerRepository;
		}


		/// <summary>
		/// Tạo thông tin thanh toán mới cho khách hàng
		/// </summary>
		public async Task<bool> createcustomerpayment(CreateCustomerPaymentModel model)
		{
			try
			{
				_logger.LogInformation("CREATE_CUSTOMER_PAYMENT_START: Tạo thông tin thanh toán - CustomerId: {CustomerId}, BankCode: {BankCode}",
					model?.CustomerId, model?.BankCode);

				// ✅ Validate dữ liệu đầu vào
				if (model == null)
				{
					_logger.LogWarning("CREATE_CUSTOMER_PAYMENT_INVALID_MODEL: Model rỗng");
					return false;
				}

				if (!model.CustomerId.HasValue || model.CustomerId <= 0)
				{
					_logger.LogWarning("CREATE_CUSTOMER_PAYMENT_INVALID_CUSTOMER_ID: CustomerId không hợp lệ - CustomerId: {CustomerId}",
						model.CustomerId);
					return false;
				}

				// ✅ Kiểm tra khách hàng có tồn tại không
				var customer = await _customerRepository.GetById(model.CustomerId.Value);
				if (customer == null || customer.DeleteStatus)
				{
					_logger.LogWarning("CREATE_CUSTOMER_PAYMENT_CUSTOMER_NOT_FOUND: Khách hàng không tồn tại - CustomerId: {CustomerId}",
						model.CustomerId);
					return false;
				}

				// ✅ Validate trường bắt buộc
				if (string.IsNullOrWhiteSpace(model.BankAccountNumber))
				{
					_logger.LogWarning("CREATE_CUSTOMER_PAYMENT_MISSING_BANK_ACCOUNT: Số tài khoản không được để trống - CustomerId: {CustomerId}",
						model.CustomerId);
					return false;
				}

				if (string.IsNullOrWhiteSpace(model.BankAccountName))
				{
					_logger.LogWarning("CREATE_CUSTOMER_PAYMENT_MISSING_ACCOUNT_NAME: Tên chủ tài khoản không được để trống - CustomerId: {CustomerId}",
						model.CustomerId);
					return false;
				}

				if (string.IsNullOrWhiteSpace(model.BankCode))
				{
					_logger.LogWarning("CREATE_CUSTOMER_PAYMENT_MISSING_BANK_CODE: Mã ngân hàng không được để trống - CustomerId: {CustomerId}",
						model.CustomerId);
					return false;
				}

				// ✅ Tạo entity mới
				var paymentInfo = new CustomerPaymentInfoEntity
				{
					CustomerId = model.CustomerId,
					BankAccountNumber = model.BankAccountNumber.Trim(),
					BankAccountName = model.BankAccountName.Trim().ToUpper(),
					BankName = model.BankName?.Trim(),
					BankCode = model.BankCode.Trim().ToUpper(),
					IsDefault = false,
					LastModifiedDate = DateTime.UtcNow,
					DeleteStatus = false
				};

				// ✅ Lưu vào database
				bool result = await _customerPaymentInfoRepository.CreateEntity(paymentInfo);

				if (result)
				{
					_logger.LogInformation("CREATE_CUSTOMER_PAYMENT_SUCCESS: Tạo thông tin thanh toán thành công - CustomerId: {CustomerId}, BankCode: {BankCode}, BankAccount: {BankAccount}",
						model.CustomerId, model.BankCode, model.BankAccountNumber);
				}
				else
				{
					_logger.LogError("CREATE_CUSTOMER_PAYMENT_FAILED: Tạo thông tin thanh toán thất bại - CustomerId: {CustomerId}",
						model.CustomerId);
				}

				return result;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "CREATE_CUSTOMER_PAYMENT_EXCEPTION: Lỗi khi tạo thông tin thanh toán - CustomerId: {CustomerId}",
					model?.CustomerId);
				return false;
			}
		}

		/// <summary>
		/// Cập nhật thông tin thanh toán của khách hàng
		/// </summary>
		public async Task<bool> updatecustomerpayment(updatecustomerpayment model)
		{
			try
			{
				_logger.LogInformation("UPDATE_CUSTOMER_PAYMENT_START: Cập nhật thông tin thanh toán - PaymentInfoId: {PaymentInfoId}, CustomerId: {CustomerId}",
					model?.Id, model?.CustomerId);

				// ✅ Validate dữ liệu đầu vào
				if (model == null)
				{
					_logger.LogWarning("UPDATE_CUSTOMER_PAYMENT_INVALID_MODEL: Model rỗng");
					return false;
				}

				if (!model.Id.HasValue || model.Id <= 0)
				{
					_logger.LogWarning("UPDATE_CUSTOMER_PAYMENT_INVALID_ID: ID không hợp lệ - Id: {Id}",
						model.Id);
					return false;
				}

				// ✅ Lấy thông tin thanh toán hiện tại
				var existingPaymentInfo = await _customerPaymentInfoRepository.GetById(model.Id.Value);
				if (existingPaymentInfo == null || existingPaymentInfo.DeleteStatus)
				{
					_logger.LogWarning("UPDATE_CUSTOMER_PAYMENT_NOT_FOUND: Thông tin thanh toán không tồn tại - PaymentInfoId: {PaymentInfoId}",
						model.Id);
					return false;
				}

				// ✅ Kiểm tra quyền: chỉ có khách hàng sở hữu hoặc admin mới được cập nhật
				if (model.CustomerId.HasValue && existingPaymentInfo.CustomerId != model.CustomerId.Value)
				{
					_logger.LogWarning("UPDATE_CUSTOMER_PAYMENT_PERMISSION_DENIED: Không có quyền cập nhật thông tin thanh toán của khách hàng khác - PaymentInfoId: {PaymentInfoId}",
						model.Id);
					return false;
				}

				// ✅ Cập nhật thông tin
				if (!string.IsNullOrWhiteSpace(model.BankAccountNumber))
					existingPaymentInfo.BankAccountNumber = model.BankAccountNumber.Trim();

				if (!string.IsNullOrWhiteSpace(model.BankAccountName))
					existingPaymentInfo.BankAccountName = model.BankAccountName.Trim().ToUpper();

				if (!string.IsNullOrWhiteSpace(model.BankName))
					existingPaymentInfo.BankName = model.BankName.Trim();

				if (!string.IsNullOrWhiteSpace(model.BankCode))
					existingPaymentInfo.BankCode = model.BankCode.Trim().ToUpper();

				existingPaymentInfo.LastModifiedDate = DateTime.UtcNow;

				// ✅ Nếu đặt làm tài khoản mặc định
				if (model.IsDefault)
				{
					existingPaymentInfo.IsDefault = model.IsDefault;
					await SetAsDefaultPaymentInfo(existingPaymentInfo.CustomerId.Value, model.Id.Value);
				}

				// ✅ Lưu vào database
				bool result = await _customerPaymentInfoRepository.UpdateEntity(existingPaymentInfo);

				if (result)
				{
					_logger.LogInformation("UPDATE_CUSTOMER_PAYMENT_SUCCESS: Cập nhật thông tin thanh toán thành công - PaymentInfoId: {PaymentInfoId}, IsDefault: {IsDefault}",
						model.Id, model.IsDefault);
				}
				else
				{
					_logger.LogError("UPDATE_CUSTOMER_PAYMENT_FAILED: Cập nhật thông tin thanh toán thất bại - PaymentInfoId: {PaymentInfoId}",
						model.Id);
				}

				return result;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "UPDATE_CUSTOMER_PAYMENT_EXCEPTION: Lỗi khi cập nhật thông tin thanh toán - PaymentInfoId: {PaymentInfoId}",
					model?.Id);
				return false;
			}
		}

		/// <summary>
		/// Xóa thông tin thanh toán của khách hàng (soft delete)
		/// </summary>
		public async Task<bool> deletecustomerpayment(deletecustomerpayment model)
		{
			try
			{
				_logger.LogInformation("DELETE_CUSTOMER_PAYMENT_START: Xóa thông tin thanh toán - PaymentInfoId: {PaymentInfoId}",
					model?.Id);

				// ✅ Validate dữ liệu đầu vào
				if (model == null)
				{
					_logger.LogWarning("DELETE_CUSTOMER_PAYMENT_INVALID_MODEL: Model rỗng");
					return false;
				}

				if (!model.Id.HasValue || model.Id <= 0)
				{
					_logger.LogWarning("DELETE_CUSTOMER_PAYMENT_INVALID_ID: ID không hợp lệ - Id: {Id}",
						model.Id);
					return false;
				}

				// ✅ Lấy thông tin thanh toán
				var paymentInfo = await _customerPaymentInfoRepository.GetById(model.Id.Value);
				if (paymentInfo == null || paymentInfo.DeleteStatus)
				{
					_logger.LogWarning("DELETE_CUSTOMER_PAYMENT_NOT_FOUND: Thông tin thanh toán không tồn tại - PaymentInfoId: {PaymentInfoId}",
						model.Id);
					return false;
				}

				// ✅ Kiểm tra nếu là tài khoản mặc định thì không cho xóa
				if (paymentInfo.IsDefault)
				{
					_logger.LogWarning("DELETE_CUSTOMER_PAYMENT_IS_DEFAULT: Không thể xóa tài khoản mặc định - PaymentInfoId: {PaymentInfoId}",
						model.Id);
					return false;
				}

				// ✅ Soft delete (đánh dấu DeleteStatus = true)
				bool result = await _customerPaymentInfoRepository.DeleteEntitiesStatus(paymentInfo);

				if (result)
				{
					_logger.LogInformation("DELETE_CUSTOMER_PAYMENT_SUCCESS: Xóa thông tin thanh toán thành công - PaymentInfoId: {PaymentInfoId}",
						model.Id);
				}
				else
				{
					_logger.LogError("DELETE_CUSTOMER_PAYMENT_FAILED: Xóa thông tin thanh toán thất bại - PaymentInfoId: {PaymentInfoId}",
						model.Id);
				}

				return result;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "DELETE_CUSTOMER_PAYMENT_EXCEPTION: Lỗi khi xóa thông tin thanh toán - PaymentInfoId: {PaymentInfoId}",
					model?.Id);
				return false;
			}
		}

		/// <summary>
		/// Lấy danh sách thông tin thanh toán của khách hàng
		/// </summary>
		public async Task<BaseDataCollection<CustomerPaymentInfoEntity>> getlistcustomerpayment(getlistcustomerpayment model)
		{
			try
			{
				_logger.LogInformation("GET_LIST_CUSTOMER_PAYMENT_START: Lấy danh sách thông tin thanh toán - CustomerId: {CustomerId}, PageNo: {PageNo}, PageSize: {PageSize}",
					model?.customerId, model?.PageNo, model?.PageSize);

				// ✅ Validate dữ liệu đầu vào
				if (model == null || !model.customerId.HasValue || model.customerId <= 0)
				{
					_logger.LogWarning("GET_LIST_CUSTOMER_PAYMENT_INVALID_CUSTOMER_ID: CustomerId không hợp lệ - CustomerId: {CustomerId}",
						model?.customerId);
					return new BaseDataCollection<CustomerPaymentInfoEntity>(
						new List<CustomerPaymentInfoEntity>(),
						0,
						model?.PageNo ?? 1,
						model?.PageSize ?? 10
					);
				}

				// ✅ Kiểm tra khách hàng có tồn tại không
				var customer = await _customerRepository.GetById(model.customerId.Value);
				if (customer == null || customer.DeleteStatus)
				{
					_logger.LogWarning("GET_LIST_CUSTOMER_PAYMENT_CUSTOMER_NOT_FOUND: Khách hàng không tồn tại - CustomerId: {CustomerId}",
						model.customerId);
					return new BaseDataCollection<CustomerPaymentInfoEntity>(
						new List<CustomerPaymentInfoEntity>(),
						0,
						model.PageNo,
						model.PageSize
					);
				}

				// ✅ Xây dựng predicate filter
				Expression<Func<CustomerPaymentInfoEntity, bool>> predicate = x =>
					x.CustomerId == model.customerId && !x.DeleteStatus;

				// ✅ Lấy danh sách thông tin thanh toán
				var allMatching = await _customerPaymentInfoRepository.FindByPredicate(predicate);
				var totalCount = allMatching.Count;

				if (totalCount == 0)
				{
					_logger.LogInformation("GET_LIST_CUSTOMER_PAYMENT_EMPTY: Khách hàng chưa có thông tin thanh toán - CustomerId: {CustomerId}",
						model.customerId);
					return new BaseDataCollection<CustomerPaymentInfoEntity>(
						new List<CustomerPaymentInfoEntity>(),
						0,
						model.PageNo,
						model.PageSize
					);
				}

				// ✅ Sắp xếp & phân trang: tài khoản mặc định đầu tiên, sau đó theo ngày cập nhật mới nhất
				var pagedData = allMatching
					.OrderByDescending(x => x.IsDefault)
					.ThenByDescending(x => x.LastModifiedDate)
					.Skip((model.PageNo - 1) * model.PageSize)
					.Take(model.PageSize)
					.ToList();

				_logger.LogInformation("GET_LIST_CUSTOMER_PAYMENT_SUCCESS: Lấy danh sách thành công - CustomerId: {CustomerId}, TotalCount: {TotalCount}, PageNo: {PageNo}, PageSize: {PageSize}",
					model.customerId, totalCount, model.PageNo, model.PageSize);

				return new BaseDataCollection<CustomerPaymentInfoEntity>(
					pagedData,
					totalCount,
					model.PageNo,
					model.PageSize
				);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GET_LIST_CUSTOMER_PAYMENT_EXCEPTION: Lỗi khi lấy danh sách thông tin thanh toán - CustomerId: {CustomerId}",
					model?.customerId);
				return new BaseDataCollection<CustomerPaymentInfoEntity>(
					new List<CustomerPaymentInfoEntity>(),
					0,
					model?.PageNo ?? 1,
					model?.PageSize ?? 10
				);
			}
		}
		#region Private Methods

		/// <summary>
		/// Đặt một tài khoản thanh toán là mặc định (bỏ mặc định từ các tài khoản khác)
		/// </summary>
		private async Task<bool> SetAsDefaultPaymentInfo(int customerId, int paymentInfoId)
		{
			try
			{
				_logger.LogInformation("SET_DEFAULT_PAYMENT_INFO_START: Đặt tài khoản mặc định - CustomerId: {CustomerId}, PaymentInfoId: {PaymentInfoId}",
					customerId, paymentInfoId);

				// ✅ Lấy tất cả tài khoản của khách hàng
				var allPaymentInfos = await _customerPaymentInfoRepository.FindByPredicate(
					x => x.CustomerId == customerId && !x.DeleteStatus);

				// ✅ Bỏ mặc định từ các tài khoản khác
				foreach (var info in allPaymentInfos)
				{
					if (info.Id != paymentInfoId && info.IsDefault)
					{
						info.IsDefault = false;
						await _customerPaymentInfoRepository.UpdateEntity(info);
					}
				}

				// ✅ Đặt tài khoản hiện tại là mặc định
				var targetPaymentInfo = allPaymentInfos.FirstOrDefault(x => x.Id == paymentInfoId);
				if (targetPaymentInfo != null)
				{
					targetPaymentInfo.IsDefault = true;
					await _customerPaymentInfoRepository.UpdateEntity(targetPaymentInfo);

					_logger.LogInformation("SET_DEFAULT_PAYMENT_INFO_SUCCESS: Đặt tài khoản mặc định thành công - PaymentInfoId: {PaymentInfoId}",
						paymentInfoId);
					return true;
				}

				return false;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "SET_DEFAULT_PAYMENT_INFO_EXCEPTION: Lỗi khi đặt tài khoản mặc định - CustomerId: {CustomerId}, PaymentInfoId: {PaymentInfoId}",
					customerId, paymentInfoId);
				return false;
			}
		}

		#endregion
	}
}
