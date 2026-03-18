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
    public interface ISessionProductService
    {
		Task<bool> create(CreateSessionProduct sessionProduct);

		Task<bool> update(UpdateSessionProduct sessionProduct);

		Task<bool> delete(DeleteSessionProduct sessionProduct);

		Task<BaseDataCollection<SessionProductEntity>> getlist(SessionProductGet sessionProduct);
	}
}
