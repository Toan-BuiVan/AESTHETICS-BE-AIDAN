using Aesthetics.Entities.Entities;
using Aesthetics.Entities.Models.RequestModel;
using Aesthetics.Entities.Models.ResponseModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Data.AestheticsInterfaces
{
    public interface IAppointmentService
    {
		Task<CreateAppointmentResponseModel> create(CreateAppointment appointment);

		Task<bool> delete(DeleteAppointment appointment);

		Task<BaseDataCollection<AppointmentResponseModel>> getlist(AppointmentGet appointment);

		/// <summary>Lấy thời gian trống của bác sĩ (kiểm tra AppointmentTimeLocks)</summary>
		Task<DoctorAvailabilityResponseModel?> GetDoctorAvailability(GetDoctorAvailabilityRequest request);

		/// <summary>
		/// Hủy đặt lịch và cập nhật trạng thái CustomerTreatmentSession
		/// Khi hủy lịch:
		/// - Xóa appointment liên quan
		/// - Cập nhật status của CustomerTreatmentSession = 4 (Cancelled)
		/// </summary>
		/// <param name="customerTreatmentSessionId">ID của CustomerTreatmentSession cần hủy</param>
		/// <returns>True nếu hủy thành công, False nếu lỗi</returns>
		Task<bool> UpdateAppointmentStatusAsync(updateappoint request);
		Task<List<ServiceInfoModel>> GetDoctorServices(int doctorId);
	}
}
