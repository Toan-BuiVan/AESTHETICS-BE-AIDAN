using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Models.RequestModel
{
    public class RequestCustomer : BaseSearchModel
    {
        public int? Id { get; set; }

        public string? FullName { get; set; }
    }
}