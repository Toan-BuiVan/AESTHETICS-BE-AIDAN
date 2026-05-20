using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Models.RequestModel
{
    public class GetPermissionListRequest : BaseSearchModel
	{
        /// <summary>Account ID để lấy danh sách permission</summary>
        public int? AccountId { get; set; }
    }

    public class UpdatePermissionRequest
    {
        /// <summary>Account ID cần cập nhật quyền</summary>
        public int AccountId { get; set; }

        /// <summary>Permission ID cần cập nhật</summary>
        public int PermissionId { get; set; }

        /// <summary>Trạng thái quyền: true = Active, false = Inactive</summary>
        public bool IsActive { get; set; }
    }
}