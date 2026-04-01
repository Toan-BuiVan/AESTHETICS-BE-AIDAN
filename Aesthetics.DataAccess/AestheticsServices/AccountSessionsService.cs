using Aesthetics.Data.AestheticsInterfaces;
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

namespace Aesthetics.Data.AestheticsServices
{
	public class AccountSessionsService : IAccountSessionsService
	{
		private readonly ILogger<AccountSessionsService> _logger;
		private readonly IAccountSessionsRepository _sessionsRepository;

		public AccountSessionsService(ILogger<AccountSessionsService> logger
			, IAccountSessionsRepository sessionsRepository)
		{
			_logger = logger;
			_sessionsRepository = sessionsRepository;
		}

		public async Task<BaseDataCollection<AccountSessionEntity>> getlist(getSession session)
		{
			try
			{
				_logger.LogInformation("Getting account sessions for AccountId: {AccountId}", session.AccountId);

				ICollection<AccountSessionEntity> sessions;

				if (session.AccountId.HasValue)
				{
					sessions = await _sessionsRepository.FindByPredicate(s => s.AccountId == session.AccountId && !s.DeleteStatus);
				}
				else
				{
					sessions = await _sessionsRepository.FindByPredicate(s => !s.DeleteStatus);
				}

				var totalCount = sessions.Count;
				var result = new BaseDataCollection<AccountSessionEntity>(
					sessions.ToList(),
					totalCount,
					1,
					totalCount > 0 ? totalCount : 1
				);

				_logger.LogInformation("Retrieved {Count} account sessions", totalCount);
				return result;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error retrieving account sessions - Exception: {Exception}", ex.Message);
				return new BaseDataCollection<AccountSessionEntity>(new List<AccountSessionEntity>(), 0, 1, 1);
			}
		}
	}
}
