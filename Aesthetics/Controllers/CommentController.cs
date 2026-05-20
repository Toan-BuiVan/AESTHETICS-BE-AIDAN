using Aesthetics.Data.AestheticsInterfaces;
using Aesthetics.Entities.Entities;
using Aesthetics.Entities.Models.RequestModel;
using Aesthetics.Entities.Models.ResponseModel;
using ASP_NetCore_Aesthetics.Filter;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aesthetics.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class CommentController : ControllerBase
	{
		private readonly ICommentService _commentService;

		public CommentController(ICommentService commentService)
		{
			_commentService = commentService;
		}

		[ServiceFilter(typeof(Filter_CheckToken))]
		[Filter_Authorization("createcomment")]
		[HttpPost("createcomment")]
		public async Task<IActionResult> Create([FromBody] RequestComment comment)
		{
			var result = await _commentService.create(comment);
			return Ok(new { success = result });
		}

		[ServiceFilter(typeof(Filter_CheckToken))]
		[Filter_Authorization("updatecomment")]
		[HttpPost("updatecomment")]
		public async Task<IActionResult> Update([FromBody] UpdateComment comment)
		{
			var result = await _commentService.update(comment);
			return Ok(new { success = result });
		}

		[ServiceFilter(typeof(Filter_CheckToken))]
		[Filter_Authorization("deletecomment")]
		[HttpDelete("deletecomment")]
		public async Task<IActionResult> Delete([FromBody] DeleteComment comment)
		{
			var result = await _commentService.delete(comment);
			return Ok(new { success = result });
		}

		[HttpPost("getcommentlist")]
		public async Task<IActionResult> GetList([FromBody] CommentGet comment)
		{
			var result = await _commentService.getlist(comment);
			return Ok(result);
		}
	}
}
