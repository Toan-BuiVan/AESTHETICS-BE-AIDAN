using System;
using System.Collections.Generic;

namespace Aesthetics.Entities.Models.ResponseModel
{
	/// <summary>Mô hình response danh sách appointment chi tiết</summary>
	public class AppointmentResponseModel
	{
		/// <summary>ID appointment</summary>
		public int Id { get; set; }

		/// <summary>Thông tin khách hàng</summary>
		public CustomerInfo? Customer { get; set; }

		/// <summary>Thông tin bác sĩ</summary>
		public StaffInfo? Staff { get; set; }

		/// <summary>Thông tin dịch vụ</summary>
		public ServiceInfo? Service { get; set; }

		/// <summary>Thông tin buổi điều trị</summary>
		public TreatmentSessionInfo? TreatmentSession { get; set; }

		/// <summary>🆕 Thông tin session điều trị của khách hàng</summary>
		public CustomerTreatmentSessionInfo? CustomerTreatmentSession { get; set; }

		/// <summary>Thời gian bắt đầu</summary>
		public DateTime? StartTime { get; set; }

		/// <summary>Thời gian kết thúc (tính toán từ duration)</summary>
		public DateTime? EndTime { get; set; }

		/// <summary>Trạng thái appointment</summary>
		public string? Status { get; set; }

		/// <summary>Giá dịch vụ (từ Service nếu mua lẻ, từ TreatmentPlan nếu mua gói)</summary>
		public decimal? Price { get; set; }

		/// <summary>Loại mua: 'Lẻ' hoặc 'Gói'</summary>
		public string? PurchaseType { get; set; }

		/// <summary>Trạng thái thanh toán</summary>
		public int PaymentStatus { get; set; }

		/// <summary>Ngày tạo</summary>
		public DateTime? CreationDate { get; set; }

		/// <summary>Email xác nhận đã gửi</summary>
		public bool IsConfirmationEmailSent { get; set; }

		/// <summary>Email nhắc nhở đã gửi</summary>
		public bool IsReminderEmailSent { get; set; }

		/// <summary>Số giờ nhắc nhở trước appointment</summary>
		public int ReminderHoursBefore { get; set; }

		/// <summary>Đã có comment cho appointment này hay chưa</summary>
		public bool IsComment { get; set; }

		/// <summary>Thông tin assignment (phòng khám)</summary>
		public AppointmentAssignmentInfo? Assignment { get; set; }
	}

	/// <summary>Thông tin khách hàng</summary>
	public class CustomerInfo
	{
		/// <summary>ID khách hàng</summary>
		public int Id { get; set; }

		/// <summary>Tên khách hàng</summary>
		public string? FullName { get; set; }

		/// <summary>Email khách hàng</summary>
		public string? Email { get; set; }

		/// <summary>Số điện thoại</summary>
		public string? PhoneNumber { get; set; }

		/// <summary>Ngày sinh</summary>
		public DateTime? DateOfBirth { get; set; }

		/// <summary>Giới tính</summary>
		public string? Gender { get; set; }
	}

	/// <summary>Thông tin bác sĩ/nhân viên</summary>
	public class StaffInfo
	{
		/// <summary>ID nhân viên</summary>
		public int Id { get; set; }

		/// <summary>Tên nhân viên</summary>
		public string? FullName { get; set; }

		/// <summary>Email</summary>
		public string? Email { get; set; }

		/// <summary>Số điện thoại</summary>
		public string? PhoneNumber { get; set; }

		/// <summary>Chuyên khoa</summary>
		public string? Specialization { get; set; }

		/// <summary>Số năm kinh nghiệm</summary>
		public int? YearsOfExperience { get; set; }
	}

	/// <summary>🆕 Thông tin session điều trị của khách hàng</summary>
	public class CustomerTreatmentSessionInfo
	{
		/// <summary>ID session điều trị của khách hàng</summary>
		public int Id { get; set; }

		/// <summary>ID liệu trình khách hàng</summary>
		public int? CustomerTreatmentPlanId { get; set; }

		/// <summary>ID buổi điều trị</summary>
		public int? TreatmentSessionId { get; set; }

		/// <summary>Trạng thái session (DaDatLich, DangThucHien, HoanThanh, KhachHuy)</summary>
		public string? Status { get; set; }

		/// <summary>Thông tin liệu trình khách hàng liên quan</summary>
		public CustomerTreatmentPlanInfo? CustomerTreatmentPlan { get; set; }
	}

	/// <summary>🆕 Thông tin liệu trình khách hàng</summary>
	public class CustomerTreatmentPlanInfo
	{
		/// <summary>ID liệu trình khách hàng</summary>
		public int Id { get; set; }

		/// <summary>ID khách hàng</summary>
		public int? CustomerId { get; set; }

		/// <summary>ID liệu trình (template)</summary>
		public int? TreatmentPlanId { get; set; }

		/// <summary>Tên liệu trình</summary>
		public string? TreatmentPlanName { get; set; }

		/// <summary>Trạng thái liệu trình (ChoDatLich, DangThucHien, HoanThanh, KhachHuy)</summary>
		public string? Status { get; set; }

		/// <summary>Số buổi trong liệu trình</summary>
		public int? TotalSessions { get; set; }
	}

	/// <summary>Thông tin dịch vụ</summary>
	public class ServiceInfo
	{
		/// <summary>ID dịch vụ</summary>
		public int Id { get; set; }

		/// <summary>Tên dịch vụ</summary>
		public string? ServiceName { get; set; }

		/// <summary>Thời lượng (phút)</summary>
		public int? Duration { get; set; }

		/// <summary>Giá dịch vụ</summary>
		public decimal? Price { get; set; }

		/// <summary>Mô tả dịch vụ</summary>
		public string? Description { get; set; }
	}

	/// <summary>Thông tin buổi điều trị</summary>
	public class TreatmentSessionInfo
	{
		/// <summary>ID buổi điều trị</summary>
		public int Id { get; set; }

		/// <summary>Tên buổi</summary>
		public string? SessionName { get; set; }

		/// <summary>Buổi thứ mấy</summary>
		public int? SessionNumber { get; set; }

		/// <summary>Thời lượng (phút)</summary>
		public int? Duration { get; set; }

		/// <summary>Mô tả buổi</summary>
		public string? Description { get; set; }
	}

	/// <summary>Thông tin phân công phòng khám</summary>
	public class AppointmentAssignmentInfo
	{
		/// <summary>ID phân công</summary>
		public int Id { get; set; }

		/// <summary>Tên phòng khám</summary>
		public string? ClinicName { get; set; }

		/// <summary>Số thứ tự</summary>
		public int? NumberOrder { get; set; }

		/// <summary>Giá dịch vụ</summary>
		public decimal? Price { get; set; }

		/// <summary>Trạng thái thanh toán</summary>
		public int PaymentStatus { get; set; }
	}

	/// <summary>Mô hình response chi tiết một appointment</summary>
	public class AppointmentDetailResponseModel
	{
		/// <summary>ID appointment</summary>
		public int Id { get; set; }

		/// <summary>Thông tin khách hàng</summary>
		public CustomerInfo? Customer { get; set; }

		/// <summary>Thông tin bác sĩ</summary>
		public StaffInfo? Staff { get; set; }

		/// <summary>Thông tin dịch vụ</summary>
		public ServiceInfo? Service { get; set; }

		/// <summary>Thông tin buổi điều trị</summary>
		public TreatmentSessionInfo? TreatmentSession { get; set; }

		/// <summary>Thời gian bắt đầu</summary>
		public DateTime? StartTime { get; set; }

		/// <summary>Thời gian kết thúc</summary>
		public DateTime? EndTime { get; set; }

		/// <summary>Trạng thái appointment (1: Booked, 2: In Progress, 3: Completed, 4: Cancelled)</summary>
		public int StatusCode { get; set; }

		/// <summary>Tên trạng thái</summary>
		public string? StatusName { get; set; }

		/// <summary>Giá dịch vụ (từ Service nếu mua lẻ, từ TreatmentPlan nếu mua gói)</summary>
		public decimal? Price { get; set; }

		/// <summary>Loại mua: 'Lẻ' hoặc 'Gói'</summary>
		public string? PurchaseType { get; set; }

		/// <summary>Trạng thái thanh toán</summary>
		public int PaymentStatus { get; set; }

		/// <summary>Ngày tạo</summary>
		public DateTime? CreationDate { get; set; }

		/// <summary>Email xác nhận đã gửi</summary>
		public bool IsConfirmationEmailSent { get; set; }

		/// <summary>Ngày gửi email xác nhận</summary>
		public DateTime? ConfirmationEmailSentDate { get; set; }

		/// <summary>Email nhắc nhở đã gửi</summary>
		public bool IsReminderEmailSent { get; set; }

		/// <summary>Ngày gửi email nhắc nhở</summary>
		public DateTime? ReminderEmailSentDate { get; set; }

		/// <summary>Số giờ nhắc nhở trước appointment</summary>
		public int ReminderHoursBefore { get; set; }

		/// <summary>Thông tin phân công phòng khám</summary>
		public AppointmentAssignmentInfo? Assignment { get; set; }
	}
}