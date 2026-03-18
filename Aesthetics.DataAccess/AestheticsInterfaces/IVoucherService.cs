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
    public interface IVoucherService
    {
		Task<bool> create(CreateVoucher voucher);

		Task<bool> update(UpdateVoucher voucher);

		Task<bool> delete(DeleteClinic voucher);

		Task<BaseDataCollection<VoucherEntity>> getlist(VoucherGet voucher);
	}
}
