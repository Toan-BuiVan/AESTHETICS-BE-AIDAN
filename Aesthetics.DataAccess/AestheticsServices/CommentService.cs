using Aesthetics.Data.AestheticsInterfaces;
using Aesthetics.Data.AestheticsInterfaces.ICommonService;
using Aesthetics.Data.RepositoryInterfaces;
using Aesthetics.Entities.Entities;
using Aesthetics.Entities.Models.RequestModel;
using Aesthetics.Entities.Models.ResponseModel;
using LinqKit;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Data.AestheticsServices
{
	public class CommentService : ICommentService
	{
		private readonly ILogger<CommentService> _logger;
		private readonly ICommentRepository _commentRepository;
		private readonly ICustomerRepository _customerRepository;
		private readonly ICommonService _commonService;

		public CommentService(
			ILogger<CommentService> logger,
			ICommentRepository commentRepository,
			ICustomerRepository customerRepository,
			ICommonService commonService)
		{
			_logger = logger;
			_commentRepository = commentRepository;
			_customerRepository = customerRepository;
			_commonService = commonService;
		}

		public async Task<bool> create(RequestComment comment)
		{
			try
			{
				// Validate ProductId and ServiceId
				if (!comment.ProductId.HasValue && !comment.ServiceId.HasValue)
				{
					_logger.LogWarning("Create Comment failed: Both ProductId and ServiceId are null");
					return false;
				}

				// Process comment image if provided
				string processedImage = null;
				if (!string.IsNullOrEmpty(comment.CommentImage))
				{
					processedImage = await _commonService.BaseProcessingFunction64(comment.CommentImage);
				}

				var entity = new CommentEntity
				{
					ProductId = comment.ProductId,
					ServiceId = comment.ServiceId,
					CustomerId = comment.CustomerId,
					CommentContent = comment.CommentContent?.Trim(),
					Rating = comment.Rating,
					CommentImage = processedImage,
					CreationDate = DateTime.UtcNow,
					DeleteStatus = false
				};

				var created = await _commentRepository.CreateEntity(entity);
				if (!created)
				{
					_logger.LogError("Create Comment failed at repository level");
					return false;
				}

				_logger.LogInformation("Create Comment success - CustomerId: {CustomerId}", comment.CustomerId);
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Create Comment exception");
				return false;
			}
		}

		public async Task<bool> delete(DeleteComment comment)
		{
			try
			{
				_logger.LogInformation("Start deleting Comment - Id: {Id}", comment.Id);
				var existingComment = await _commentRepository.GetById(comment.Id);
				if (existingComment == null)
				{
					_logger.LogWarning("Delete Comment failed: Not found with Id {Id}", comment.Id);
					return false;
				}

				var deleted = await _commentRepository.DeleteEntity(existingComment);
				if (!deleted)
				{
					_logger.LogError("Delete Comment failed at repository level: Id {Id}", comment.Id);
					return false;
				}

				_logger.LogInformation("Delete Comment success: Id {Id}", comment.Id);
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Delete Comment exception: Id {Id}", comment.Id);
				return false;
			}
		}

		public async Task<BaseDataCollection<CommentResponseModel>> getlist(CommentGet searchComment)
		{
			try
			{
				Expression<Func<CommentEntity, bool>> predicate = x => x.DeleteStatus != true;

				if (searchComment.ProductId.HasValue && searchComment.ProductId.Value > 0)
				{
					predicate = predicate.And(x => x.ProductId == searchComment.ProductId.Value);
				}

				if (searchComment.ServiceId.HasValue && searchComment.ServiceId.Value > 0)
				{
					predicate = predicate.And(x => x.ServiceId == searchComment.ServiceId.Value);
				}

				var allMatching = await _commentRepository.FindByPredicate(predicate);
				var allMatchingList = allMatching.ToList();
				var totalCount = allMatchingList.Count;

				// Get all unique CustomerIds
				var customerIds = allMatchingList
					.Where(x => x.CustomerId.HasValue)
					.Select(x => x.CustomerId.Value)
					.Distinct()
					.ToList();

				// Load all customers in batch
				var customers = new Dictionary<int, CustomerEntity>();
				if (customerIds.Any())
				{
					var customersList = await _customerRepository.FindByPredicate(x => customerIds.Contains(x.Id));
					customers = customersList.ToDictionary(x => x.Id, x => x);
				}

				// Apply customers to comments
				foreach (var commentEntity in allMatchingList)
				{
					if (commentEntity.CustomerId.HasValue && customers.ContainsKey(commentEntity.CustomerId.Value))
					{
						commentEntity.Customer = customers[commentEntity.CustomerId.Value];
					}
				}

				// Map to response model and apply pagination
				var pagedData = allMatchingList
					.OrderByDescending(x => x.CreationDate)
					.Skip((searchComment.PageNo - 1) * searchComment.PageSize)
					.Take(searchComment.PageSize)
					.Select(x => new CommentResponseModel
					{
						Id = x.Id,
						ProductId = x.ProductId,
						ServiceId = x.ServiceId,
						CustomerId = x.CustomerId,
						CustomerName = x.Customer?.FullName,
						CommentContent = x.CommentContent,
						Rating = x.Rating,
						CommentImage = x.CommentImage,
						CreationDate = x.CreationDate
					})
					.ToList();

				return new BaseDataCollection<CommentResponseModel>(pagedData, totalCount, searchComment.PageNo, searchComment.PageSize);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GetList Comment exception");
				return new BaseDataCollection<CommentResponseModel>(null, 0, searchComment.PageNo, searchComment.PageSize);
			}
		}

		public async Task<bool> update(UpdateComment comment)
		{
			try
			{
				var existingComment = await _commentRepository.GetById(comment.Id);
				if (existingComment == null)
				{
					_logger.LogWarning("Update Comment failed: Not found with Id {Id}", comment.Id);
					return false;
				}

				if (!string.IsNullOrEmpty(comment.CommentContent))
				{
					existingComment.CommentContent = comment.CommentContent.Trim();
				}

				// Process comment image if provided
				if (!string.IsNullOrEmpty(comment.CommentImage))
				{
					existingComment.CommentImage = await _commonService.BaseProcessingFunction64(comment.CommentImage);
				}

				if (comment.Rating != null)
				{
					existingComment.Rating = comment.Rating;
				}

				var updated = await _commentRepository.UpdateEntity(existingComment);
				if (!updated)
				{
					_logger.LogError("Update Comment failed at repository level: Id {Id}", comment.Id);
					return false;
				}

				_logger.LogInformation("Update Comment success: Id {Id}", comment.Id);
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Update Comment exception: Id {Id}", comment.Id);
				return false;
			}
		}
	}
}