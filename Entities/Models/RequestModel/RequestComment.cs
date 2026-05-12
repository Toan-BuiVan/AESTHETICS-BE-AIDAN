using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Models.RequestModel
{
    public class RequestComment
    {
		public int? ProductId { get; set; }
		public int? ServiceId { get; set; }
		public int CustomerId { get; set; }
		public string? CommentContent { get; set; }
		public string? CommentImage { get; set; }
		public int Rating { get; set; } = 1;
		public int? DoctorId { get; set; }
		public int? TreatmentSessionsId { get; set; }
		public int? AppointmentId { get; set; }
	}

	public class UpdateComment
	{
		public int Id { get; set; }
		public string? CommentContent { get; set; }
		public string? CommentImage { get; set; }
		public int Rating { get; set; } = 1;
	}

	public class DeleteComment
	{
		public int Id { get; set; }
	}

	public class CommentGet : BaseSearchModel
	{
		public int? ProductId { get; set; }
		public int? ServiceId { get; set; }
		public int? DoctorId { get; set; }
		public int? TreatmentSessionsId { get; set; }
	}
}
