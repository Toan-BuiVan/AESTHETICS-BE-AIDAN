using System;
using System.Collections.Generic;

namespace Aesthetics.Entities.Models.ResponseModel
{
	/// <summary>Mô hình response thời gian trống của bác sĩ</summary>
	public class DoctorAvailabilityResponseModel
	{
		/// <summary>ID bác sĩ</summary>
		public int DoctorId { get; set; }

		/// <summary>Tên bác sĩ</summary>
		public string? DoctorName { get; set; }

		/// <summary>ID dịch vụ</summary>
		public int ServiceId { get; set; }

		/// <summary>Tên dịch vụ</summary>
		public string? ServiceName { get; set; }

		/// <summary>Thời lượng dịch vụ (phút)</summary>
		public int ServiceDuration { get; set; }

		/// <summary>Ngày được kiểm tra</summary>
		public DateTime Date { get; set; }

		/// <summary>Danh sách các khoảng thời gian trống</summary>
		public List<AvailableTimeSlot>? AvailableTimeSlots { get; set; }

		/// <summary>Tổng số khoảng thời gian trống</summary>
		public int TotalAvailableSlots { get; set; }

		/// <summary>✅ Số lượng lịch hẹn hiện tại của bác sĩ trong ngày</summary>
		public int CurrentAppointmentCount { get; set; }

		/// <summary>✅ Giới hạn số lịch hẹn tối đa mỗi ngày</summary>
		public int MaxDailyLimit { get; set; }

		/// <summary>✅ Còn bao nhiêu slot có thể đặt</summary>
		public int RemainingSlots { get; set; }

		/// <summary>✅ Bác sĩ đã đạt giới hạn chưa</summary>
		public bool IsLimitReached { get; set; }
	}

	/// <summary>Mô hình khoảng thời gian trống</summary>
	public class AvailableTimeSlot
	{
		/// <summary>Thời gian bắt đầu (HH:mm)</summary>
		public string? StartTime { get; set; }

		/// <summary>Thời gian kết thúc (HH:mm)</summary>
		public string? EndTime { get; set; }

		/// <summary>Có thể đặt lịch hay không</summary>
		public bool IsAvailable { get; set; } = true;
	}
}