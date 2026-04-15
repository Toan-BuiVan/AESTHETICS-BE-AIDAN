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
	public interface IRefundServcie
	{
		public Task<bool> createrefundservice(CreateRefundModel model);
		public Task<bool> updaterefundservice(UpdtaeRefundModel model);
		public Task<BaseDataCollection<RefundEntity>> getlistrefund(getlist model);
	}
}
