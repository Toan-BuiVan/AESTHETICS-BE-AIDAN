using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Models.ResponseModel
{
	public class BaseDataCollection<T>
	{
		public BaseDataCollection()
		{

		}
		public BaseDataCollection(List<T>? data, int totalCount, int pageNo, int pageSize)
		{
			BaseDatas = data;
			TotalRecordCount = totalCount;
			PageIndex = pageNo;
			PageCount = (int)Math.Ceiling((decimal)totalCount / pageSize);
		}
		public List<T>? BaseDatas { get; set; }
		public int TotalRecordCount { get; set; }
		public int PageIndex { get; set; }
		public int PageCount { get; set; }
	}
}
