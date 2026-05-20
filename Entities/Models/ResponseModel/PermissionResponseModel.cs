using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Models.ResponseModel
{
    public class PermissionResponseModel
    {
        public int Id { get; set; }
        public int? AccountId { get; set; }
        public int? FunctionId { get; set; }
        public bool IsActive { get; set; }
        public string? FunctionCode { get; set; }
        public string? FunctionName { get; set; }
        public string? Description { get; set; }
    }
}