using System;

namespace Aesthetics.Entities.Models.ResponseModel
{
    /// <summary>
    /// Response model khi tạo đặt lịch thành công
    /// </summary>
    public class CreateAppointmentResponseModel
    {
        /// <summary>ID đặt lịch vừa tạo</summary>
        public int AppointmentId { get; set; }

        /// <summary>ID hóa đơn liên quan</summary>
        public int InvoiceId { get; set; }

        /// <summary>Trạng thái</summary>
        public string Status { get; set; }

        /// <summary>Thời gian bắt đầu</summary>
        public DateTime? StartTime { get; set; }

        /// <summary>Thông báo</summary>
        public string Message { get; set; }
    }
}