using Aesthetics.Data.AestheticsInterfaces;
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

		public CommentService(ILogger<CommentService> logger, ICommentRepository commentRepository)
		{
			_logger = logger;
			_commentRepository = commentRepository;
		}

		public async Task<bool> create(RequestComment comment)
		{
			try
			{
				var entity = new CommentEntity
				{
					ProductId = comment.ProductId ?? 0,
					ServiceId = comment.ServiceId ?? 0,
					CustomerId = comment.CustomerId,
					CommentContent = comment.CommentContent?.Trim(),
					Rating = comment.Rating,
					DeleteStatus = false
				};

				var created = await _commentRepository.CreateEntity(entity);
				if (!created)
				{
					_logger.LogError("Create Comment failed at repository level");
					return false;
				}

				_logger.LogInformation("Create Comment success");
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
				_logger.LogInformation("Start deleting Comment");
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

		public async Task<BaseDataCollection<CommentEntity>> getlist(CommentGet searchComment)
		{
			try
			{
				Expression<Func<CommentEntity, bool>> predicate = x => true;

				if (searchComment.ProductId.HasValue)
				{
					predicate = predicate.And(x => x.ProductId == searchComment.ProductId.Value);
				}

				if (searchComment.ServiceId.HasValue)
				{
					predicate = predicate.And(x => x.ServiceId == searchComment.ServiceId.Value);
				}

				var allMatching = await _commentRepository.FindByPredicate(predicate);
				var totalCount = allMatching.Count;

				var pagedData = allMatching
					.OrderByDescending(x => x.Id) 
					.Skip((searchComment.PageNo - 1) * searchComment.PageSize)
					.Take(searchComment.PageSize)
					.ToList();

				return new BaseDataCollection<CommentEntity>(pagedData, totalCount, searchComment.PageNo, searchComment.PageSize);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GetList Comment exception");
				return new BaseDataCollection<CommentEntity>(null, 0, searchComment.PageNo, searchComment.PageSize);
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

				existingComment.CommentContent = comment.CommentContent.Trim();

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
