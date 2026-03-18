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
    public interface ICartProductService
    {
		Task<bool> create(CreateCartProduct request);

		Task<bool> update(UpdateCartProduct request);

		Task<bool> delete(DeleteCartProduct request);

		Task<BaseDataCollection<CartProductEntity>> getlist(GetCartProduct request);
	}
}
