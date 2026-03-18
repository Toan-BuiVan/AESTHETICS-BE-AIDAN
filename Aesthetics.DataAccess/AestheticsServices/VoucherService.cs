using Aesthetics.Data.AestheticsInterfaces;
using Aesthetics.Data.AestheticsInterfaces.ICommonService;
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
using Microsoft.AspNetCore.Hosting;
using System.Linq.Expressions;
using XAct;

namespace Aesthetics.Data.AestheticsServices
{
    public class VoucherService : IVoucherService
	{
		private readonly ILogger<VoucherService> _logger;
		private readonly IVoucherRepository _voucherRepository;
		private ICommonService _commonService;
		public VoucherService(ILogger<VoucherService> logger
			, IVoucherRepository voucherRepository
			, ICommonService commonService)
		{
			_logger = logger;
			_voucherRepository = voucherRepository;
			_commonService = commonService;
		}

		public async Task<bool> create(CreateVoucher voucher)
		{
			try
			{
				if (voucher == null 
					|| string.IsNullOrWhiteSpace(voucher.Description) 
					|| voucher.DiscountValue <= 0 || voucher.DiscountValue > 100 
					|| voucher.StartDate < DateTime.Today 
					|| voucher.EndDate < DateTime.Today 
					|| voucher.StartDate > voucher.EndDate 
					|| voucher.MinimumOrderValue <= 0 
					|| voucher.MaxValue <= 0 
					|| voucher.AccumulatedPoints < 0 
					|| voucher.RatingPoints < 0 
					|| string.IsNullOrWhiteSpace(voucher.RankMember) 
					|| string.IsNullOrWhiteSpace(voucher.VoucherImage))
				{
					return false;
				}
				var vouchersImagePath = await _commonService.BaseProcessingFunction64(voucher.VoucherImage);
				var code = await _voucherRepository.GenCodeUnique();
				var newVouchers = new VoucherEntity
				{
					Code = code,
					Description = voucher.Description,
					VoucherImage = vouchersImagePath,
					DiscountValue = voucher.DiscountValue,
					StartDate = voucher.StartDate,
					EndDate = voucher.EndDate,
					MinimumOrderValue = voucher.MinimumOrderValue,
					MaxValue = voucher.MaxValue,
					RankMember = voucher.RankMember,
					RatingPoints = voucher.RatingPoints,
					AccumulatedPoints = voucher.AccumulatedPoints,
					IsActive = true
				};
				await _voucherRepository.CreateEntity(newVouchers);
				return true;
			}
			catch (Exception ex)
			{
				throw new Exception($"Error in voucherVouchers Message: {ex.Message} | StackTrace: {ex.StackTrace}", ex);
			}
		}

		public async Task<bool> delete(DeleteClinic voucher)
		{
			try
			{
				_logger.LogInformation("Start deleting Voucher");

				var existingVoucher = await _voucherRepository.GetById(voucher.Id);
				if (existingVoucher == null)
				{
					_logger.LogWarning("Delete Voucher failed: Not found with Id {Id}", voucher.Id);
					return false;
				}

				var deleted = await _voucherRepository.DeleteRangeEntitiesStatus(existingVoucher);
				if (!deleted)
				{
					_logger.LogError("Delete Voucher failed at repository level: Id {Id}", voucher.Id);
					return false;
				}

				_logger.LogInformation("Delete Voucher success: Id {Id}", voucher.Id);
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Delete Voucher exception: Id {Id}", voucher.Id);
				return false;
			}
		}

		public async Task<BaseDataCollection<VoucherEntity>> getlist(VoucherGet voucher)
		{
			try
			{
				// Base predicate: not deleted, active, and currently valid (within date range)
				DateTime currentDate = DateTime.UtcNow;
				var allMatching = await _voucherRepository.FindByPredicate(x =>
					x.DeleteStatus == false &&
					x.IsActive == true &&
					(x.StartDate == null || x.StartDate <= currentDate) &&
					(x.EndDate == null || x.EndDate >= currentDate));

				if (!string.IsNullOrWhiteSpace(voucher.Code))
				{
					var code = voucher.Code.ToLower();
					allMatching = allMatching
						.Where(x => x.Code != null && x.Code.ToLower().Contains(code))
						.ToList();
				}

				if (voucher.StartDate.HasValue)
				{
					allMatching = allMatching
						.Where(x => x.StartDate >= voucher.StartDate.Value)
						.ToList();
				}

				if (voucher.EndDate.HasValue)
				{
					allMatching = allMatching
						.Where(x => x.EndDate <= voucher.EndDate.Value)
						.ToList();
				}

				if (!string.IsNullOrWhiteSpace(voucher.RankMember))
				{
					var rank = voucher.RankMember.ToLower();
					allMatching = allMatching
						.Where(x => x.RankMember != null && x.RankMember.ToLower().Contains(rank))
						.ToList();
				}

				var totalCount = allMatching.Count;
				var pagedData = allMatching
					.OrderBy(x => x.Id)
					.Skip((voucher.PageNo - 1) * voucher.PageSize)
					.Take(voucher.PageSize)
					.ToList();

				return new BaseDataCollection<VoucherEntity>(
					pagedData,
					totalCount,
					voucher.PageNo,
					voucher.PageSize
				);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GetList Voucher exception");
				return new BaseDataCollection<VoucherEntity>(
					null,
					0,
					voucher.PageNo,
					voucher.PageSize
				);
			}
		}


		public async Task<bool> update(UpdateVoucher voucher)
		{
			try
			{
				if (voucher == null
					|| string.IsNullOrWhiteSpace(voucher.Description)
					|| voucher.DiscountValue <= 0 || voucher.DiscountValue > 100
					|| voucher.StartDate > voucher.EndDate               
					|| voucher.MinimumOrderValue <= 0
					|| voucher.MaxValue <= 0
					|| voucher.AccumulatedPoints < 0
					|| voucher.RatingPoints < 0
					|| string.IsNullOrWhiteSpace(voucher.RankMember))
				{
					return false;
				}

				var existing = await _voucherRepository.GetById(voucher.Id);   
				if (existing == null)
				{
					return false;
				}

				string vouchersImagePath = existing.VoucherImage;
				if (!string.IsNullOrWhiteSpace(voucher.VoucherImage))
				{
					vouchersImagePath = await _commonService.BaseProcessingFunction64(voucher.VoucherImage);
				}

				existing.Description = voucher.Description;
				existing.DiscountValue = voucher.DiscountValue;
				existing.StartDate = voucher.StartDate;
				existing.EndDate = voucher.EndDate;
				existing.MinimumOrderValue = voucher.MinimumOrderValue;
				existing.MaxValue = voucher.MaxValue;
				existing.RankMember = voucher.RankMember;
				existing.RatingPoints = voucher.RatingPoints ?? 0;
				existing.AccumulatedPoints = voucher.AccumulatedPoints ?? 0;
				existing.VoucherImage = vouchersImagePath;
				existing.IsActive = voucher.IsActive ?? false;

				await _voucherRepository.UpdateEntity(existing);  

				return true;
			}
			catch (Exception ex)
			{
				throw new Exception($"Error in update Voucher Message: {ex.Message} | StackTrace: {ex.StackTrace}", ex);
			}
		}
	}
}
