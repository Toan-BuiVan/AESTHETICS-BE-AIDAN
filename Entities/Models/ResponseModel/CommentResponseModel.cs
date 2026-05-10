using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Models.ResponseModel
{
	public class CommentResponseModel
	{
		public int? Id { get; set; }
		public int? ProductId { get; set; }
		public int? ServiceId { get; set; }
		public int? CustomerId { get; set; }
		public string? CustomerName { get; set; }
		public string? CommentContent { get; set; }
		public int? Rating { get; set; }
		public string? CommentImage { get; set; }
		public DateTime? CreationDate { get; set; }
		public int? DoctorId { get; set; }
		public int? TreatmentSessionsId { get; set; }
	}
}
