using Aesthetics.Data.AestheticsInterfaces;
using Aesthetics.Data.RepositoryInterfaces;
using Aesthetics.Entities.Entities;
using Aesthetics.Entities.Models.RequestModel.DHN;
using Aesthetics.Entities.Models.ResponseModel;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Aesthetics.Data.AestheticsServices
{
	public class AddressInfoService : IAddressInfoService
	{
		private readonly ILogger<AddressInfoService> _logger;
		private readonly IAddressInfoRepository _addressInfoRepository;

		public AddressInfoService(ILogger<AddressInfoService> logger, IAddressInfoRepository addressInfoRepository)
		{
			_logger = logger;
			_addressInfoRepository = addressInfoRepository;
		}

		public async Task<bool> CreateAddressAsync(CreateAddressInfoModel model)
		{
			try
			{
				_logger.LogInformation("CREATE_ADDRESS_START: Tạo địa chỉ giao hàng mới - CustomerId: {CustomerId}",
					model?.CustomerId);

				// ✅ Validate dữ liệu đầu vào
				if (model == null || model.CustomerId <= 0)
				{
					_logger.LogWarning("CREATE_ADDRESS_INVALID_CUSTOMER_ID: CustomerId không hợp lệ - CustomerId: {CustomerId}",
						model?.CustomerId);
					return false;
				}

				// ✅ Validate trường bắt buộc
				if (string.IsNullOrWhiteSpace(model.DetailAddress))
				{
					_logger.LogWarning("CREATE_ADDRESS_MISSING_DETAIL: Địa chỉ chi tiết không được để trống - CustomerId: {CustomerId}",
						model.CustomerId);
					return false;
				}

				bool isDefault = model.IsDefault ?? false;

				var entity = new AddressInfoEntity
				{
					CustomerId = model.CustomerId,
					ProvinceId = model.ProvinceId,
					ProvinceName = model.ProvinceName?.Trim(),
					DistrictId = model.DistrictId,
					DistrictName = model.DistrictName?.Trim(),
					WardCode = model.WardCode?.Trim(),
					WardName = model.WardName?.Trim(),
					DetailAddress = model.DetailAddress?.Trim(),
					IsDefault = isDefault,
					CreatedAt = DateTime.UtcNow,
					DeleteStatus = false
				};

				// ✅ Nếu đặt làm địa chỉ mặc định
				if (isDefault)
				{
					await SetAsDefaultAddress(model.CustomerId);
				}

				// ✅ Lưu vào database
				var created = await _addressInfoRepository.CreateEntity(entity);
				if (!created)
				{
					_logger.LogError("CREATE_ADDRESS_FAILED: Tạo địa chỉ thất bại ở repository - CustomerId: {CustomerId}",
						model.CustomerId);
					return false;
				}

				_logger.LogInformation("CREATE_ADDRESS_SUCCESS: Tạo địa chỉ thành công - CustomerId: {CustomerId}, IsDefault: {IsDefault}",
					model.CustomerId, isDefault);
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "CREATE_ADDRESS_EXCEPTION: Lỗi khi tạo địa chỉ - CustomerId: {CustomerId}",
					model?.CustomerId);
				return false;
			}
		}

		public async Task<bool> UpdateAddressAsync(UpdateAddressInfoModel model)
		{
			try
			{
				_logger.LogInformation("UPDATE_ADDRESS_START: Cập nhật địa chỉ giao hàng - AddressId: {AddressId}",
					model?.Id);

				// ✅ Validate dữ liệu đầu vào
				if (model == null || model.Id <= 0)
				{
					_logger.LogWarning("UPDATE_ADDRESS_INVALID_ID: ID không hợp lệ - Id: {Id}",
						model?.Id);
					return false;
				}

				// ✅ Lấy địa chỉ hiện tại
				var existingAddress = await _addressInfoRepository.GetById(model.Id);
				if (existingAddress == null || existingAddress.DeleteStatus)
				{
					_logger.LogWarning("UPDATE_ADDRESS_NOT_FOUND: Địa chỉ không tồn tại - AddressId: {AddressId}",
						model.Id);
					return false;
				}

				// ✅ Validate trường bắt buộc
				if (!string.IsNullOrWhiteSpace(model.DetailAddress))
				{
					existingAddress.DetailAddress = model.DetailAddress.Trim();
				}

				// ✅ Cập nhật thông tin địa chỉ
				if (model.ProvinceId > 0)
					existingAddress.ProvinceId = model.ProvinceId;

				if (!string.IsNullOrWhiteSpace(model.ProvinceName))
					existingAddress.ProvinceName = model.ProvinceName.Trim();

				if (model.DistrictId > 0)
					existingAddress.DistrictId = model.DistrictId;

				if (!string.IsNullOrWhiteSpace(model.DistrictName))
					existingAddress.DistrictName = model.DistrictName.Trim();

				if (!string.IsNullOrWhiteSpace(model.WardCode))
					existingAddress.WardCode = model.WardCode.Trim();

				if (!string.IsNullOrWhiteSpace(model.WardName))
					existingAddress.WardName = model.WardName.Trim();

				// ✅ Xử lý IsDefault: nếu đặt làm mặc định thì bỏ mặc định từ các địa chỉ khác
				bool modelIsDefault = model.IsDefault ?? false;
				bool currentIsDefault = existingAddress.IsDefault ?? false;

				if (modelIsDefault && !currentIsDefault)
				{
					existingAddress.IsDefault = true;
					await SetAsDefaultAddress(existingAddress.CustomerId.Value, model.Id);
				}
				else if (!modelIsDefault && currentIsDefault)
				{
					existingAddress.IsDefault = false;
				}

				// ✅ Lưu vào database
				var updated = await _addressInfoRepository.UpdateEntity(existingAddress);
				if (!updated)
				{
					_logger.LogError("UPDATE_ADDRESS_FAILED: Cập nhật địa chỉ thất bại ở repository - AddressId: {AddressId}",
						model.Id);
					return false;
				}

				_logger.LogInformation("UPDATE_ADDRESS_SUCCESS: Cập nhật địa chỉ thành công - AddressId: {AddressId}, IsDefault: {IsDefault}",
					model.Id, existingAddress.IsDefault);
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "UPDATE_ADDRESS_EXCEPTION: Lỗi khi cập nhật địa chỉ - AddressId: {AddressId}",
					model?.Id);
				return false;
			}
		}

		public async Task<bool> DeleteAddressAsync(int addressId)
		{
			try
			{
				_logger.LogInformation("DELETE_ADDRESS_START: Xóa địa chỉ giao hàng - AddressId: {AddressId}",
					addressId);

				// ✅ Validate dữ liệu đầu vào
				if (addressId <= 0)
				{
					_logger.LogWarning("DELETE_ADDRESS_INVALID_ID: ID không hợp lệ - AddressId: {AddressId}",
						addressId);
					return false;
				}

				// ✅ Lấy địa chỉ
				var existingAddress = await _addressInfoRepository.GetById(addressId);
				if (existingAddress == null || existingAddress.DeleteStatus)
				{
					_logger.LogWarning("DELETE_ADDRESS_NOT_FOUND: Địa chỉ không tồn tại - AddressId: {AddressId}",
						addressId);
					return false;
				}

				int customerId = existingAddress.CustomerId.Value;
				bool isDefault = existingAddress.IsDefault ?? false;

				// ✅ Nếu đang xóa địa chỉ mặc định, đặt địa chỉ khác làm mặc định
				if (isDefault)
				{
					var otherAddresses = await _addressInfoRepository.FindByPredicate(
						x => x.CustomerId == customerId && x.Id != addressId && x.DeleteStatus != true);

					if (otherAddresses.Any())
					{
						var newDefaultAddress = otherAddresses.FirstOrDefault();
						if (newDefaultAddress != null)
						{
							newDefaultAddress.IsDefault = true;
							await _addressInfoRepository.UpdateEntity(newDefaultAddress);
							_logger.LogInformation("DELETE_ADDRESS_SET_NEW_DEFAULT: Đặt địa chỉ khác làm mặc định - NewDefaultAddressId: {NewDefaultAddressId}",
								newDefaultAddress.Id);
						}
					}
				}

				// ✅ Soft delete
				var deleted = await _addressInfoRepository.DeleteRangeEntitiesStatus(existingAddress);
				if (!deleted)
				{
					_logger.LogError("DELETE_ADDRESS_FAILED: Xóa địa chỉ thất bại ở repository - AddressId: {AddressId}",
						addressId);
					return false;
				}

				_logger.LogInformation("DELETE_ADDRESS_SUCCESS: Xóa địa chỉ thành công - AddressId: {AddressId}",
					addressId);
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "DELETE_ADDRESS_EXCEPTION: Lỗi khi xóa địa chỉ - AddressId: {AddressId}",
					addressId);
				return false;
			}
		}

		public async Task<BaseDataCollection<AddressInfoEntity>> GetAddressesListAsync(GetAddressInfoModel model)
		{
			try
			{
				_logger.LogInformation("GET_LIST_ADDRESSES_START: Lấy danh sách địa chỉ - CustomerId: {CustomerId}, PageNo: {PageNo}, PageSize: {PageSize}",
					model?.CustomerId, model?.PageNo, model?.PageSize);

				// ✅ Validate dữ liệu đầu vào
				if (model == null)
				{
					_logger.LogWarning("GET_LIST_ADDRESSES_INVALID_MODEL: Model rỗng");
					return new BaseDataCollection<AddressInfoEntity>(null, 0, 1, 10);
				}

				// ✅ Xây dựng predicate filter
				Expression<Func<AddressInfoEntity, bool>> predicate = x => x.DeleteStatus != true;

				if (model.CustomerId.HasValue && model.CustomerId.Value > 0)
				{
					predicate = x => x.CustomerId == model.CustomerId.Value && x.DeleteStatus != true;
				}

				// ✅ Lấy danh sách
				var allMatching = await _addressInfoRepository.FindByPredicate(predicate);
				var totalCount = allMatching.Count;

				if (totalCount == 0)
				{
					_logger.LogInformation("GET_LIST_ADDRESSES_EMPTY: Khách hàng chưa có địa chỉ - CustomerId: {CustomerId}",
						model.CustomerId);
					return new BaseDataCollection<AddressInfoEntity>(
						null,
						0,
						model.PageNo,
						model.PageSize
					);
				}

				// ✅ Sắp xếp & phân trang: địa chỉ mặc định đầu tiên, sau đó theo ngày tạo mới nhất
				var pagedData = allMatching
					.OrderByDescending(x => x.IsDefault)
					.ThenByDescending(x => x.CreatedAt)
					.Skip((model.PageNo - 1) * model.PageSize)
					.Take(model.PageSize)
					.ToList();

				_logger.LogInformation("GET_LIST_ADDRESSES_SUCCESS: Lấy danh sách thành công - CustomerId: {CustomerId}, TotalCount: {TotalCount}, PageNo: {PageNo}, PageSize: {PageSize}",
					model.CustomerId, totalCount, model.PageNo, model.PageSize);

				return new BaseDataCollection<AddressInfoEntity>(
					pagedData,
					totalCount,
					model.PageNo,
					model.PageSize
				);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GET_LIST_ADDRESSES_EXCEPTION: Lỗi khi lấy danh sách địa chỉ - CustomerId: {CustomerId}",
					model?.CustomerId);
				return new BaseDataCollection<AddressInfoEntity>(
					null,
					0,
					model?.PageNo ?? 1,
					model?.PageSize ?? 10
				);
			}
		}

		#region Private Methods

		/// <summary>
		/// Đặt một địa chỉ là mặc định (bỏ mặc định từ các địa chỉ khác của khách hàng)
		/// </summary>
		private async Task<bool> SetAsDefaultAddress(int customerId, int? excludeAddressId = null)
		{
			try
			{
				_logger.LogInformation("SET_DEFAULT_ADDRESS_START: Đặt địa chỉ mặc định - CustomerId: {CustomerId}, ExcludeAddressId: {ExcludeAddressId}",
					customerId, excludeAddressId);

				// ✅ Lấy tất cả địa chỉ của khách hàng
				var allAddresses = await _addressInfoRepository.FindByPredicate(
					x => x.CustomerId == customerId && x.DeleteStatus != true);

				// ✅ Bỏ mặc định từ các địa chỉ khác
				foreach (var address in allAddresses)
				{
					bool addressIsDefault = address.IsDefault ?? false;
					if ((excludeAddressId == null || address.Id != excludeAddressId) && addressIsDefault)
					{
						address.IsDefault = false;
						await _addressInfoRepository.UpdateEntity(address);
						_logger.LogInformation("SET_DEFAULT_ADDRESS_REMOVED: Bỏ mặc định từ địa chỉ - AddressId: {AddressId}",
							address.Id);
					}
				}

				_logger.LogInformation("SET_DEFAULT_ADDRESS_SUCCESS: Đặt địa chỉ mặc định thành công - CustomerId: {CustomerId}",
					customerId);
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "SET_DEFAULT_ADDRESS_EXCEPTION: Lỗi khi đặt địa chỉ mặc định - CustomerId: {CustomerId}",
					customerId);
				return false;
			}
		}

		#endregion
	}
}