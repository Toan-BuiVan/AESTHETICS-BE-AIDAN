using Aesthetics.Data.AestheticsInterfaces;
using Aesthetics.Data.RepositoryInterfaces;
using Aesthetics.Data.RepositoryServices;
using Aesthetics.Entities.Entities;
using Aesthetics.Entities.Enum;
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
    public class WalletService : IWalletService
	{
		private readonly IWalletRepository _walletRepository;
		private readonly ILogger<WalletService> _logger;
		private readonly ICustomerRepository _customerRepository;
		private readonly IVoucherRepository _voucherRepository;
		public WalletService(IWalletRepository walletRepository
			, ILogger<WalletService> logger
			, ICustomerRepository customerRepository
			, IVoucherRepository voucherRepository)
		{
			_walletRepository = walletRepository;
			_logger = logger;
			_customerRepository = customerRepository;
			_voucherRepository = voucherRepository;
		}

		public async Task<bool> create(CreateWallet wallet)
		{
			try
			{
				var checkWallets = await _walletRepository.GetWalletById(wallet.VoucherId, wallet.CustomerId);
				var findCustomer = await _customerRepository.GetById(wallet.CustomerId);
				var findVouchers = await _voucherRepository.GetById(wallet.VoucherId);

				if (checkWallets || findCustomer == null || findVouchers == null)
					return false;

				var customerRank = RankHelper.ParseRank(findCustomer.RankMember);
				var voucherRank = RankHelper.ParseRank(findVouchers.RankMember);

				if (customerRank == null || voucherRank == null)
				{
					_logger.LogWarning("Invalid rank for Customer {CustomerId} or Voucher {VoucherId}",
						wallet.CustomerId, wallet.VoucherId);
					return false;
				}

				if (!RankHelper.CanUseVoucher(customerRank.Value, voucherRank.Value))
				{
					_logger.LogWarning("Customer rank {CustomerRank} cannot use voucher with rank {VoucherRank}",
						customerRank, voucherRank);
					return false;
				}

				var newWallets = new WalletEntity
				{
					CustomerId = wallet.CustomerId,
					VoucherId = wallet.VoucherId,
					IsUsed = false,
					ClaimedDate = DateTime.Now,
					DeleteStatus = false
				};

				await _walletRepository.CreateEntity(newWallets);
				_logger.LogInformation("Wallet created successfully for Customer {CustomerId} with Voucher {VoucherId}",
					wallet.CustomerId, wallet.VoucherId);
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in create Wallet");
				throw new Exception($"Error in create Wallet Message: {ex.Message} | StackTrace: {ex.StackTrace}", ex);
			}
		}

		public async Task<bool> delete(DeleteWallest wallet)
		{
			try
			{
				_logger.LogInformation("Start deleting Wallet");

				var existingWallet = await _walletRepository.GetById(wallet.WalletsID);
				if (existingWallet == null)
				{
					_logger.LogWarning("Delete Wallet failed: Not found with Id {Id}", wallet.WalletsID);
					return false;
				}

				var deleted = await _walletRepository.DeleteRangeEntitiesStatus(existingWallet);
				if (!deleted)
				{
					_logger.LogError("Delete Wallet failed at repository level: Id {Id}", wallet.WalletsID);
					return false;
				}

				_logger.LogInformation("Delete Wallet success: Id {Id}", wallet.WalletsID);
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Delete Wallet exception: Id {Id}", wallet.WalletsID);
				return false;
			}
		}

		public async Task<BaseDataCollection<WalletResponseModel>> getlist(WalletGet searchWallet)
		{
			try
			{
				// ✅ Build predicate: not deleted and unused vouchers only
				Expression<Func<WalletEntity, bool>> predicate = x =>
					x.DeleteStatus != true && x.IsUsed == false;

				if (searchWallet.CustomerId > 0)
				{
					// When filtering by CustomerId
					predicate = x =>
						x.CustomerId == searchWallet.CustomerId &&
						x.DeleteStatus != true &&
						x.IsUsed == false;
				}

				// ✅ Lấy wallet với include Voucher
				var allMatching = await _walletRepository.GetWalletsByPredicateWithVoucherAsync(predicate);

				var totalCount = allMatching.Count;

				// ✅ Mapping sang WalletResponseDTO + Phân trang
				var pagedData = allMatching
					.OrderBy(x => x.CustomerId)
					.Skip((searchWallet.PageNo - 1) * searchWallet.PageSize)
					.Take(searchWallet.PageSize)
					.Select(w => new WalletResponseModel
					{
						Id = w.Id,
						CustomerId = w.CustomerId,
						VoucherId = w.VoucherId,
						ClaimedDate = w.ClaimedDate,
						IsUsed = w.IsUsed,
						// Thông tin Voucher
						VoucherCode = w.Voucher?.Code,
						VoucherDescription = w.Voucher?.Description,
						DiscountValue = w.Voucher?.DiscountValue,
						StartDate = w.Voucher?.StartDate,
						EndDate = w.Voucher?.EndDate,
						MinimumOrderValue = w.Voucher?.MinimumOrderValue,
						MaxValue = w.Voucher?.MaxValue,
						RankMember = w.Voucher?.RankMember,
						IsActive = w.Voucher?.IsActive ?? false
					})
					.ToList();

				_logger.LogInformation("GetList Wallet success: Total {Total}, Page {PageNo}/{PageSize}",
					totalCount, searchWallet.PageNo, searchWallet.PageSize);

				return new BaseDataCollection<WalletResponseModel>(
					pagedData,
					totalCount,
					searchWallet.PageNo,
					searchWallet.PageSize
				);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GetList Wallet exception");
				return new BaseDataCollection<WalletResponseModel>(
					null,
					0,
					searchWallet.PageNo,
					searchWallet.PageSize
				);
			}
		}

		/// <summary>
		/// ✅ Đổi voucher bằng điểm (AccumulatedPoints hoặc RatingPoints)
		/// Người rank thấp có thể dùng điểm để đổi voucher ở rank cao hơn
		/// </summary>
		public async Task<bool> ExchangeVoucherAsync(RequestExchangeVoucher request)
		{
			try
			{
				_logger.LogInformation("ExchangeVoucher started: CustomerId {CustomerId}, VoucherId {VoucherId}, PointType {PointType}",
					request.CustomerId, request.VoucherId, request.PointType);

				// ✅ Validate request
				if (request.CustomerId <= 0 || request.VoucherId <= 0)
				{
					_logger.LogWarning("ExchangeVoucher failed: Invalid CustomerId {CustomerId} or VoucherId {VoucherId}",
						request.CustomerId, request.VoucherId);
					return false;
				}

				if (request.PointType < 0 || request.PointType > 1)
				{
					_logger.LogWarning("ExchangeVoucher failed: Invalid PointType {PointType}", request.PointType);
					return false;
				}

				// ✅ Lấy thông tin customer
				var customer = await _customerRepository.GetById(request.CustomerId);
				if (customer == null)
				{
					_logger.LogWarning("ExchangeVoucher failed: Customer not found {CustomerId}", request.CustomerId);
					return false;
				}

				// ✅ Lấy thông tin voucher
				var voucher = await _voucherRepository.GetById(request.VoucherId);
				if (voucher == null)
				{
					_logger.LogWarning("ExchangeVoucher failed: Voucher not found {VoucherId}", request.VoucherId);
					return false;
				}

				// ✅ Kiểm tra voucher còn hiệu lực
				if (!voucher.IsActive)
				{
					_logger.LogWarning("ExchangeVoucher failed: Voucher {VoucherId} is not active", request.VoucherId);
					return false;
				}

				// ✅ Kiểm tra voucher còn trong hạn
				var now = DateTime.Now;
				if (voucher.StartDate.HasValue && now < voucher.StartDate.Value)
				{
					_logger.LogWarning("ExchangeVoucher failed: Voucher {VoucherId} not available yet", request.VoucherId);
					return false;
				}

				if (voucher.EndDate.HasValue && now > voucher.EndDate.Value)
				{
					_logger.LogWarning("ExchangeVoucher failed: Voucher {VoucherId} expired", request.VoucherId);
					return false;
				}

				// ✅ Kiểm tra rank customer có thể dùng voucher không
				var customerRank = RankHelper.ParseRank(customer.RankMember);
				var voucherRank = RankHelper.ParseRank(voucher.RankMember);

				if (voucherRank.HasValue && !RankHelper.CanUseVoucher(customerRank.GetValueOrDefault(), voucherRank.Value))
				{
					_logger.LogWarning("ExchangeVoucher failed: Customer rank {CustomerRank} cannot use voucher rank {VoucherRank}",
						customer.RankMember, voucher.RankMember);
					return false;
				}

				// ✅ Xác định loại điểm và kiểm tra điểm có đủ không
				int requiredPoints = 0;
				int currentPoints = 0;
				string pointTypeName = "";

				if (request.PointType == 0) // AccumulatedPoints (điểm giới thiệu)
				{
					requiredPoints = voucher.AccumulatedPoints;
					currentPoints = customer.AccumulatedPoints;
					pointTypeName = "Accumulated Points";
				}
				else if (request.PointType == 1) // RatingPoints (điểm mua hàng)
				{
					requiredPoints = voucher.RatingPoints;
					currentPoints = customer.RatingPoints;
					pointTypeName = "Rating Points";
				}

				_logger.LogInformation("ExchangeVoucher: Customer {CustomerId} has {CurrentPoints} {PointType}, needs {RequiredPoints}",
					request.CustomerId, currentPoints, pointTypeName, requiredPoints);

				// ✅ Kiểm tra điểm có đủ không
				if (currentPoints < requiredPoints)
				{
					_logger.LogWarning("ExchangeVoucher failed: Insufficient {PointType} for Customer {CustomerId}. Have {Current}, Need {Required}",
						pointTypeName, request.CustomerId, currentPoints, requiredPoints);
					return false;
				}

				// ✅ Kiểm tra customer chưa có voucher này trong wallet chưa
				var existingWallet = await _walletRepository.GetWalletById(request.VoucherId, request.CustomerId);
				if (existingWallet)
				{
					_logger.LogWarning("ExchangeVoucher failed: Customer {CustomerId} already has voucher {VoucherId}",
						request.CustomerId, request.VoucherId);
					return false;
				}

				// ✅ Trừ điểm từ customer
				if (request.PointType == 0) // AccumulatedPoints
				{
					customer.AccumulatedPoints -= requiredPoints;
				}
				else if (request.PointType == 1) // RatingPoints
				{
					customer.RatingPoints -= requiredPoints;
				}

				var customerUpdated = await _customerRepository.UpdateEntity(customer);
				if (!customerUpdated)
				{
					_logger.LogError("ExchangeVoucher failed: Failed to update customer points for CustomerId {CustomerId}", request.CustomerId);
					return false;
				}

				_logger.LogInformation("ExchangeVoucher: Points deducted successfully. {PointType} deducted {RequiredPoints}",
					pointTypeName, requiredPoints);

				// ✅ Tạo wallet mới cho customer
				var newWallet = new WalletEntity
				{
					CustomerId = request.CustomerId,
					VoucherId = request.VoucherId,
					IsUsed = false,
					ClaimedDate = DateTime.Now,
					DeleteStatus = false
				};

				var walletCreated = await _walletRepository.CreateEntity(newWallet);
				if (!walletCreated)
				{
					_logger.LogError("ExchangeVoucher failed: Failed to create wallet for CustomerId {CustomerId}", request.CustomerId);
					return false;
				}

				// ✅ Success!
				_logger.LogInformation("ExchangeVoucher completed successfully: WalletId {WalletId}, CustomerId {CustomerId}, VoucherId {VoucherId}",
					newWallet.Id, request.CustomerId, request.VoucherId);

				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "ExchangeVoucher exception: CustomerId {CustomerId}, VoucherId {VoucherId}",
					request.CustomerId, request.VoucherId);
				return false;
			}
		}
	}
}
