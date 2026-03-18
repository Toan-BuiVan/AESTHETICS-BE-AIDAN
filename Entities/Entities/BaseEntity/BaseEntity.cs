using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.BaseEntity
{
    public class BaseEntity
    {
		[Key]
		public int Id { get; set; }
		/// <summary>
		/// false = Active, 
		/// true = Deleted
		/// </summary>
		public bool DeleteStatus { get; set; }
	}
}
