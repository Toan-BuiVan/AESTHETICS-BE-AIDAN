using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Models.RequestModel
{
	public class BaseSearchModel
	{
		public int PageNo { get; set; } = 1; 
		public int PageSize { get; set; } = 10; 
	}
}
