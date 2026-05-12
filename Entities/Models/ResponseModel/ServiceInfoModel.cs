using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Models.ResponseModel
{
    /// <summary>
    /// Model chứa thông tin dịch vụ mà bác sĩ khám
    /// </summary>
    public class ServiceInfoModel
    {
        /// <summary>ID dịch vụ</summary>
        public int? Id { get; set; }

        /// <summary>Tên dịch vụ</summary>
        public string? ServiceName { get; set; }

        /// <summary>Thời lượng dịch vụ (phút)</summary>
        public int? Duration { get; set; }

        /// <summary>Giá dịch vụ</summary>
        public decimal? Price { get; set; }

        /// <summary>Mô tả dịch vụ</summary>
        public string? Description { get; set; }

		/// <summary>Ảnh dịch vụ</summary>
		public string? ServiceImage { get; set; }

		/// <summary>ID loại dịch vụ</summary>
		public int? ServiceTypeId { get; set; }

        /// <summary>Là liệu trình hay dịch vụ đơn lẻ</summary>
        public bool? IsCourse { get; set; }

        /// <summary>Số lần bác sĩ đã khám dịch vụ này</summary>
        public int AppointmentCount { get; set; }
    }
}