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
    public interface ITreatmentPlanService
    {
		Task<bool> create(CreateTreatmentPlan plan);

		Task<bool> update(UpdateTreatmentPlan plan);

		Task<bool> delete(DeleteTreatmentPlan plan);

		Task<BaseDataCollection<TreatmentPlanResponseModel>> getlist(TreatmentPlanGet searchPlan);
	}
}
