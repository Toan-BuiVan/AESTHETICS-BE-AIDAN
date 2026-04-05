using Aesthetics.Entities.Models.ResponseModel.AI;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aesthetics.Data.AestheticsInterfaces.AI
{
    public interface IAIAppointmentService
    {
        /// <summary>Bài 1-2: Lấy slot trống của bác sĩ trong một ngày</summary>
        Task<AIAvailableSlotsResponse> GetDoctorAvailableSlotsAsync(int staffId, DateTime date, int? serviceId = null);

        /// <summary>Bài 1: Lấy slot trống của bác sĩ cho liệu trình cụ thể</summary>
        Task<AIAvailableSlotsResponse> GetDoctorAvailableSlotsForTreatmentPlanAsync(int staffId, int treatmentPlanId, DateTime date);

        /// <summary>Bài 3: Lấy danh sách bác sĩ của một dịch vụ (chỉ bác sĩ có appointment)</summary>
        Task<AIServiceDoctorsResponse> GetDoctorsForServiceAsync(int serviceId);

        /// <summary>Lấy danh sách TẤT CẢ bác sĩ của một liệu trình</summary>
        Task<AIServiceDoctorsResponse> GetDoctorsForTreatmentPlanAsync(int treatmentPlanId);

		/// <summary>Bài 7-8: Đặt lịch khám (hỗ trợ đặt lịch cho buổi cụ thể trong liệu trình)</summary>
		Task<AIBookAppointmentResponse> BookAppointmentAsync(
			int customerId,
			int staffId,
			int serviceId,
			DateTime appointmentDate,
			string appointmentTime,
			int? treatmentPlanId = null,
			int? sessionNumber = null);

		/// <summary>Bài 9-11: Hủy lịch hẹn</summary>
		Task<AICancelAppointmentResponse> CancelAppointmentAsync(int customerId, int staffId, DateTime? appointmentDate = null, int? serviceId = null);

        /// <summary>Tìm bác sĩ của dịch vụ và trả về slot trống cho ngày cụ thể</summary>
        Task<AIAvailableSlotsForServiceResponse> GetAvailableSlotsForServiceAsync(int serviceId, DateTime date, int? treatmentPlanId = null);
    }
}