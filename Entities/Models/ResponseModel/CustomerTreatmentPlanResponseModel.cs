using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Models.ResponseModel
{
	/// <summary>
	/// Response model cho GetList CustomerTreatmentPlan - trả về đầy đủ thông tin chi tiết
	/// </summary>
	public class CustomerTreatmentPlanResponseModel
	{
		/// <summary>Thông tin gói liệu trình của khách hàng</summary>
		public CustomerTreatmentPlanInformation? CustomerTreatmentPlanInformation { get; set; }

		/// <summary>Thông tin gói liệu trình template (gốc)</summary>
		public TreatmentPlanInfomation? TreatmentPlanInformation { get; set; }

		/// <summary>Thông tin dịch vụ</summary>
		public ServiceInfomation? ServiceInformation { get; set; }

		/// <summary>Danh sách các buổi chữa trị của khách hàng</summary>
		public List<CustomerSessionInformation>? CustomerSessions { get; set; }
	}

	/// <summary>
	/// Thông tin cơ bản của CustomerTreatmentPlan
	/// </summary>
	public class CustomerTreatmentPlanInformation
	{
		/// <summary>ID gói liệu trình của khách</summary>
		public int? Id { get; set; }

		/// <summary>FK → Customers: khách nào đăng ký</summary>
		public int? CustomerId { get; set; }

		/// <summary>FK → TreatmentPlans: gói nào</summary>
		public int? TreatmentPlanId { get; set; }

		/// <summary>
		/// Trạng thái: DangThucHien, HoanThanh, TamDung, Huy
		/// </summary>
		public string? Status { get; set; }
	}

	/// <summary>
	/// Thông tin buổi chữa trị của khách hàng
	/// </summary>
	public class CustomerSessionInformation
	{
		/// <summary>ID của CustomerTreatmentSession</summary>
		public int? CustomerSessionId { get; set; }

		/// <summary>ID buổi template (TreatmentSession)</summary>
		public int? TreatmentSessionId { get; set; }

		/// <summary>Số thứ tự buổi: 1, 2, 3...</summary>
		public int? SessionNumber { get; set; }

		/// <summary>Tên buổi: 'Buổi 1: Tẩy da chết', 'Buổi 2: Laser nhẹ'</summary>
		public string? SessionName { get; set; }

		/// <summary>Mô tả chi tiết quy trình buổi</summary>
		public string? Description { get; set; }

		/// <summary>Thời lượng buổi (phút)</summary>
		public int? Duration { get; set; }

		/// <summary>
		/// Trạng thái buổi: ChuaThucHien, ChoDatLich, DaDatLich, DangThucHien, HoanThanh, BoLo
		/// </summary>
		public string? Status { get; set; }

		/// <summary>Danh sách sản phẩm sử dụng trong buổi này</summary>
		public List<SessionProductInformation>? Products { get; set; }
	}
}
