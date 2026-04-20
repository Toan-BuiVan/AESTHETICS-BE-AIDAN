using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Aesthetics.Data.AestheticsInterfaces.GHN
{
	public interface IGHNService
	{
		Task<JsonDocument> GetProvincesAsync();
		Task<JsonDocument> GetDistrictsAsync(int provinceId);
		Task<JsonDocument> GetWardsAsync(int districtId);
		Task<JsonDocument> GetAvailableServicesAsync(int fromDistrict, int toDistrict, int shopId = 6387655);
		Task<JsonDocument> CalculateShippingFeeAsync(CalculateShippingFeeRequest request, int shopId = 6387655);
	}

	public class CalculateShippingFeeRequest
	{
		/// <summary>
		/// ID của gói dịch vụ (lấy được từ API available-services)
		/// Nếu không điền, sử dụng ServiceTypeId thay thế
		/// </summary>
		public int? ServiceId { get; set; }

		/// <summary>
		/// Loại dịch vụ: 1 = Express, 2 = Standard, 3 = Saving
		/// Sử dụng khi không có ServiceId
		/// </summary>
		public int? ServiceTypeId { get; set; }

		/// <summary>
		/// Giá trị của sản phẩm (VND). GHN tính tiền bảo hiểm dựa vào giá trị này
		/// </summary>
		public int InsuranceValue { get; set; }

		/// <summary>
		/// Mã giảm giá của GHN. Nếu không có, để rỗng hoặc null
		/// </summary>
		public string Coupon { get; set; }

		/// <summary>
		/// ID Phường/Xã người nhận
		/// </summary>
		public string ToWardCode { get; set; }

		/// <summary>
		/// ID Quận/Huyện người nhận
		/// </summary>
		public int ToDistrictId { get; set; }

		/// <summary>
		/// ID Quận/Huyện người gửi
		/// </summary>
		public int FromDistrictId { get; set; }

		/// <summary>
		/// Trọng lượng hàng hóa (gram). Mặc định: 500
		/// </summary>
		public int Weight { get; set; } = 500;

		/// <summary>
		/// Chiều dài (cm). Mặc định: 15
		/// </summary>
		public int Length { get; set; } = 15;

		/// <summary>
		/// Chiều rộng (cm). Mặc định: 15
		/// </summary>
		public int Width { get; set; } = 15;

		/// <summary>
		/// Chiều cao (cm). Mặc định: 15
		/// </summary>
		public int Height { get; set; } = 15;
	}
}
