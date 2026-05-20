using Aesthetics.Data.AestheticsInterfaces;
using Aesthetics.Data.RepositoryInterfaces;
using Aesthetics.Entities.Entities;
using Aesthetics.Entities.Models.RequestModel;
using Aesthetics.Entities.Models.ResponseModel;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Aesthetics.Data.AestheticsServices
{
	public class PermissionService : IPermissionService
	{
		private readonly ILogger<PermissionService> _logger;
		private readonly IAccountRepository _accountRepository;
		private readonly IPermissionsRepository _permissionsRepository;
		private readonly IFunctionsRepository _functionsRepository;

		public PermissionService(
			ILogger<PermissionService> logger,
			IAccountRepository accountRepository,
			IPermissionsRepository permissionsRepository,
			IFunctionsRepository functionsRepository)
		{
			_logger = logger;
			_accountRepository = accountRepository;
			_permissionsRepository = permissionsRepository;
			_functionsRepository = functionsRepository;
		}

		public async Task<BaseDataCollection<PermissionResponseModel>> GetPermissionListAsync(GetPermissionListRequest request)
		{
			try
			{
				_logger.LogInformation("Start GetPermissionListAsync for AccountId: {AccountId}", request.AccountId);

				// Validate AccountId
				if (request.AccountId <= 0)
				{
					_logger.LogWarning("GetPermissionListAsync failed: Invalid AccountId {AccountId}", request.AccountId);
					return new BaseDataCollection<PermissionResponseModel>(new List<PermissionResponseModel>(), 0, request.PageNo, request.PageSize);
				}

				// Check if account exists
				var account = await _accountRepository.GetById(request.AccountId ?? 0);
				if (account == null)
				{
					_logger.LogWarning("GetPermissionListAsync failed: Account not found with Id {AccountId}", request.AccountId);
					return new BaseDataCollection<PermissionResponseModel>(new List<PermissionResponseModel>(), 0, request.PageNo, request.PageSize);
				}

				// Get permissions using common repository
				var permissions = await _permissionsRepository.FindByPredicate(
					p => p.AccountId == request.AccountId && p.DeleteStatus == false);

				var totalCount = permissions.Count;

				// Apply paging
				var pagedPermissions = permissions
					.OrderBy(p => p.FunctionId)
					.Skip((request.PageNo - 1) * request.PageSize)
					.Take(request.PageSize)
					.ToList();

				// Get all function IDs từ permissions
				var functionIds = pagedPermissions
					.Where(p => p.FunctionId.HasValue)
					.Select(p => p.FunctionId.Value)
					.Distinct()
					.ToList();

				// Fetch tất cả functions một lần (tránh N+1 query)
				var functions = new Dictionary<int, FunctionEntity>();
				if (functionIds.Count > 0)
				{
					var functionList = await _functionsRepository.FindByPredicate(
						f => functionIds.Contains(f.Id) && f.DeleteStatus == false);
					functions = functionList.ToDictionary(f => f.Id, f => f);
				}

				// Map to response model
				var result = pagedPermissions.Select(p =>
				{
					var function = p.FunctionId.HasValue && functions.ContainsKey(p.FunctionId.Value)
						? functions[p.FunctionId.Value]
						: null;

					return new PermissionResponseModel
					{
						Id = p.Id,
						AccountId = p.AccountId,
						FunctionId = p.FunctionId,
						IsActive = p.IsActive,
						FunctionCode = function?.FunctionCode,
						FunctionName = function?.FunctionName,
						Description = function?.Description
					};
				}).ToList();

				_logger.LogInformation("GetPermissionListAsync success: Found {Count} permissions for AccountId {AccountId}", result.Count, request.AccountId);
				return new BaseDataCollection<PermissionResponseModel>(result, totalCount, request.PageNo, request.PageSize);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GetPermissionListAsync Exception: AccountId {AccountId}", request.AccountId);
				return new BaseDataCollection<PermissionResponseModel>(new List<PermissionResponseModel>(), 0, request.PageNo, request.PageSize);
			}
		}
		public async Task<bool> UpdatePermissionAsync(UpdatePermissionRequest request)
		{
			try
			{
				_logger.LogInformation("Start UpdatePermissionAsync for AccountId: {AccountId}, PermissionId: {PermissionId}",
					request.AccountId, request.PermissionId);

				// Validate input
				if (request.AccountId <= 0 || request.PermissionId <= 0)
				{
					_logger.LogWarning("UpdatePermissionAsync failed: Invalid AccountId {AccountId} or PermissionId {PermissionId}",
						request.AccountId, request.PermissionId);
					return false;
				}

				// Check if account exists
				var account = await _accountRepository.User_GetByID(request.AccountId);
				if (account == null)
				{
					_logger.LogWarning("UpdatePermissionAsync failed: Account not found with Id {AccountId}", request.AccountId);
					return false;
				}

				// Get permission by id
				var permission = await _permissionsRepository.GetById(request.PermissionId);
				if (permission == null || permission.AccountId != request.AccountId || permission.DeleteStatus == true)
				{
					_logger.LogWarning("UpdatePermissionAsync failed: Permission {PermissionId} not found or does not belong to AccountId {AccountId}",
						request.PermissionId, request.AccountId);
					return false;
				}

				// Update permission
				permission.IsActive = request.IsActive;
				var result = await _permissionsRepository.UpdateEntity(permission);

				_logger.LogInformation("UpdatePermissionAsync success: {Result}", result);
				return result;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "UpdatePermissionAsync Exception: AccountId {AccountId}, PermissionId {PermissionId}",
					request.AccountId, request.PermissionId);
				return false;
			}
		}
	}
}