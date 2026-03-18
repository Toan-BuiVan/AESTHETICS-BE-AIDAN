using Aesthetics.Data.RepositoryInterfaces;
using Aesthetics.Data.RepositoryServices.Common;
using Aesthetics.Entities.Entities;
using Aesthetics.Entities.Models.RequestModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XSystem.Security.Cryptography;

namespace Aesthetics.Data.RepositoryServices
{
	public class AuthenticationRepository : CommonRepository<AccountSessionEntity>, IAuthenticationRepository
	{
		public AuthenticationRepository(ILogger<AuthenticationRepository> logger, AestheticsDbContext.AestheticsDbContext dbContext) : base(logger, dbContext)
		{

		}

		public async Task<bool> DeleteAccountSession(string? token, int? userID)
		{
			var entity = await _dbContext.AccountSessions.FirstOrDefaultAsync(s => s.Token == token && s.Id == userID);

			if (entity == null)
				return false;

			_dbContext.AccountSessions.Remove(entity);
			await _dbContext.SaveChangesAsync();
			return true;
		}

		public async Task<bool> DeleteAccountSessionAll(int? userID)
		{
			if (userID == null)
				return false;

			var entities = await _dbContext.AccountSessions
				.Where(s => s.Id == userID)
				.ToListAsync();

			if (!entities.Any())
				return false;

			_dbContext.AccountSessions.RemoveRange(entities);
			await _dbContext.SaveChangesAsync();

			return true;
		}

		public async Task<AccountEntity> GetUserByUserName(string UserName)
		{
			return _dbContext.Accounts.ToList().Where(s => s.UserName == UserName).FirstOrDefault();
		}

		public async Task<AccountEntity?> login(RequestLogin request)
		{
			try
			{
				var passwordHash = Security.EncryptPassWord(request.Password);
				var user = await _dbContext.Accounts
					.Where(s => s.UserName == request.UserName
							 && s.PassWord == passwordHash
							 && s.DeleteStatus == false)
					.FirstOrDefaultAsync();

				return user;
			}
			catch (Exception)
			{
				throw;
			}
		}

		public async Task<int> UpdateRefeshToken(int id, string RefeshToken, DateTime RefeshTokenExpiryTime)
		{
			var user = _dbContext.Accounts.Where(s => s.Id == id).FirstOrDefault();
			if (user == null)
			{
				return -1;
			}
			user.RefreshToken = RefeshToken;
			user.TokenExpired = RefeshTokenExpiryTime;
			_dbContext.Accounts.Update(user);
			_dbContext.SaveChanges();
			return -1;
		}
	}

	public static class Security
	{
		public static string EncryptPassWord(string randomString)
		{
			var crypt = new SHA256Managed();
			string hash = String.Empty;
			byte[] crypto = crypt.ComputeHash(Encoding.ASCII.GetBytes(randomString));
			foreach (byte theByte in crypto)
			{
				hash += theByte.ToString("x2");
			}
			return hash;
		}
	}
}
