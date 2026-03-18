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
    public class ClinicService : IClinicService
	{
		private readonly ILogger<ClinicService> _logger;
		private readonly IClinicRepository _clinicRepository;

		public ClinicService(ILogger<ClinicService> logger, IClinicRepository clinicRepository)
		{
			_logger = logger;
			_clinicRepository = clinicRepository;
		}

		public async Task<bool> create(RequestClinic clinic)
		{
			try
			{
				var existingClinic = await _clinicRepository.GetByName(clinic.ClinicName);
				if (existingClinic != null)
				{
					_logger.LogWarning("Create Clinic failed: ClinicName {ClinicName}", clinic.ClinicName);
					return false;
				}

				var entity = new ClinicEntity
				{
					ClinicName = clinic.ClinicName.Trim(),
					ClinicStatus = true,
					DeleteStatus = false
				};

				var created = await _clinicRepository.CreateEntity(entity);
				if (!created)
				{
					_logger.LogError("Create Clinic failed at repository level: {ClinicName}", clinic.ClinicName);
					return false;
				}

				_logger.LogInformation("Create Clinic success: {ClinicName}", clinic.ClinicName);
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Create Clinic exception: ClinicName {ClinicName}", clinic.ClinicName);
				return false;
			}
		}

		public async Task<bool> delete(DeleteClinic clinic)
		{
			try
			{
				_logger.LogInformation("Start deleting Clinic");

				var existingClinic = await _clinicRepository.GetById(clinic.Id);
				if (existingClinic == null)
				{
					_logger.LogWarning("Delete Clinic failed: Not found with Id {Id}", clinic.Id);
					return false;
				}

				var deleted = await _clinicRepository.DeleteRangeEntitiesStatus(existingClinic);
				if (!deleted)
				{
					_logger.LogError("Delete Clinic failed at repository level: Id {Id}", clinic.Id);
					return false;
				}

				_logger.LogInformation("Delete Clinic success: Id {Id}", clinic.Id);
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Delete Clinic exception: Id {Id}", clinic.Id);
				return false;
			}
		}

		public async Task<BaseDataCollection<ClinicEntity>> getlist(ClinicGet searchClinic)
		{
			try
			{
				Expression<Func<ClinicEntity, bool>> predicate = x => x.DeleteStatus != true;

				if (searchClinic.Id.HasValue)
				{
					predicate = x => x.Id == searchClinic.Id.Value && x.DeleteStatus != true;
				}

				if (!string.IsNullOrWhiteSpace(searchClinic.ClinicName))
				{
					var name = searchClinic.ClinicName.ToLower();
					predicate = x =>
						x.ClinicName.ToLower().Contains(name)
						&& x.DeleteStatus != true;
				}

				var allMatching = await _clinicRepository.FindByPredicate(predicate);
				var totalCount = allMatching.Count;

				var pagedData = allMatching
					.OrderBy(x => x.ClinicName)
					.Skip((searchClinic.PageNo - 1) * searchClinic.PageSize)
					.Take(searchClinic.PageSize)
					.ToList();

				return new BaseDataCollection<ClinicEntity>(
					pagedData,
					totalCount,
					searchClinic.PageNo,
					searchClinic.PageSize
				);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GetList Clinic exception");
				return new BaseDataCollection<ClinicEntity>(
					null,
					0,
					searchClinic.PageNo,
					searchClinic.PageSize
				);
			}
		}

		public async Task<bool> update(UpdateClinic clinic)
		{
			try
			{
				var existingClinic = await _clinicRepository.GetById(clinic.Id);
				if (existingClinic == null)
				{
					_logger.LogWarning("Update Clinic failed: Not found with Id {Id}", clinic.Id);
					return false;
				}

				if (!string.IsNullOrWhiteSpace(clinic.ClinicName)
					&& existingClinic.ClinicName != clinic.ClinicName)
				{
					var duplicate = await _clinicRepository.GetByName(clinic.ClinicName);
					if (duplicate != null)
					{
						_logger.LogWarning("Update Clinic failed: Duplicate ClinicName {ClinicName}", clinic.ClinicName);
						return false;
					}

					existingClinic.ClinicName = clinic.ClinicName.Trim();
				}

				if (clinic.ClinicStatus.HasValue)
				{
					existingClinic.ClinicStatus = clinic.ClinicStatus.Value;
				}

				var updated = await _clinicRepository.UpdateEntity(existingClinic);
				if (!updated)
				{
					_logger.LogError("Update Clinic failed at repository level: Id {Id}", clinic.Id);
					return false;
				}

				_logger.LogInformation("Update Clinic success: Id {Id}", clinic.Id);
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Update Clinic exception: Id {Id}", clinic.Id);
				return false;
			}
		}
	}
}
