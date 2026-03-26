using Aesthetics.Entities.Entities;
using Aesthetics.Entities.Models.RequestModel;
using Aesthetics.Entities.Models.ResponseModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Data.AestheticsInterfaces
{
    public interface ICommentService
    {
		Task<bool> create(RequestComment comment);

		Task<bool> update(UpdateComment comment);

		Task<bool> delete(DeleteComment comment);

		Task<BaseDataCollection<CommentResponseModel>> getlist(CommentGet comment);
	}
}
