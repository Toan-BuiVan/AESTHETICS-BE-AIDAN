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
    public interface IEquipmentService
    {
		Task<bool> create(RequestEquipment equipment);

		Task<bool> update(UpdateEquipment equipment);

		Task<bool> delete(DeleteEquipment equipment);

		Task<BaseDataCollection<EquipmentEntity>> getlist(EquipmentGet equipment);
	}
}
