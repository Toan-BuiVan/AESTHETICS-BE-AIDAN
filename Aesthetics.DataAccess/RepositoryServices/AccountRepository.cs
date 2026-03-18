using Aesthetics.Data.RepositoryInterfaces;
using Aesthetics.Data.RepositoryServices.Common;
using Aesthetics.Entities.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aesthetics.Data.RepositoryServices
{
    public class AccountRepository : CommonRepository<AccountEntity>, IAccountRepository
	{
		public AccountRepository(ILogger<CommonRepository<AccountEntity>> logger, AestheticsDbContext.AestheticsDbContext dbContext) : base(logger, dbContext)
		{

		}

		public async Task<string> GenerateUniqueReferralCode()
		{
			string referralCode;
			bool exists;
			Random random = new Random();
			const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

			do
			{
				referralCode = new string(chars.OrderBy(x => random.Next()).Take(5).ToArray());
				exists = await _dbContext.Customers.AnyAsync(s => s.ReferralCode == referralCode);
			} while (exists);

			return referralCode;
		}

		public async Task<AccountEntity?> GetByName(string name)
		{
			try
			{
				var clinic = await _dbContext.Accounts
					.AsNoTracking()
					.FirstOrDefaultAsync(x => x.UserName.ToLower() == name.ToLower());
				return clinic;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GetByName Exception: UserName '{UserName}'", name);
				return null;
			}
		}

		public async Task<FunctionEntity?> GetFunctionIDByName(string functionCode)
		{
			return await _dbContext.Functions.Where(s => s.FunctionCode == functionCode).FirstOrDefaultAsync();
		}

		public async Task<PermissionEntity?> GetPermisstionUserIDOfFunctionID(int userId, int functionId)
		{
			try
			{
				return await _dbContext.Permissions
					.AsNoTracking()
					.FirstOrDefaultAsync(s => s.AccountId == userId && s.FunctionId == functionId && s.DeleteStatus == false);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GetPermisstionUserIDOfFunctionID Exception: UserId '{UserId}', FunctionId '{FunctionId}'", userId, functionId);
				return null;
			}
		}


		public async Task<AccountEntity?> GetUser_ByUserName(string userName)
		{
			try
			{
				return await _dbContext.Accounts
					.AsNoTracking()
					.FirstOrDefaultAsync(x => x.UserName.ToLower() == userName.ToLower() && x.DeleteStatus == false);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GetUser_ByUserName Exception: UserName '{UserName}'", userName);
				return null;
			}
		}

		public async Task<bool> UserUpdate_RefeshToken(int userId, string refreshToken, DateTime expiry)
		{
			try
			{
				var user = await _dbContext.Accounts.FirstOrDefaultAsync(x => x.Id == userId);
				if (user == null) return false;

				user.RefreshToken = refreshToken;
				user.TokenExpired = expiry;

				_dbContext.Accounts.Update(user);
				await _dbContext.SaveChangesAsync();
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "UserUpdate_RefeshToken Exception: UserId '{UserId}'", userId);
				return false;
			}
		}

		public async Task<AccountEntity?> User_GetByID(int id)
		{
			try
			{
				return await _dbContext.Accounts
					.AsNoTracking()
					.FirstOrDefaultAsync(x => x.Id == id && x.DeleteStatus == false);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "User_GetByID Exception: Id '{Id}'", id);
				return null;
			}
		}

	}
}
