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
		Task<JsonDocument> CalculateShippingFeesAsync(CreateShippingOrderRequest createShippingOrder, int fromDistrict = 2194, int shopId = 6387655);
		Task<JsonDocument> CreateShippingOrdersAsync(CreateShippingOrderRequest createShippingOrder, int fromDistrict = 2194, int shopId = 6387655);
		
		//Task<JsonDocument> GetAvailableServicesAsync(int customerId, int fromDistrict, int shopId = 6387655);
		//Task<JsonDocument> CalculateShippingFeeAsync(CreateShippingOrderRequest createshippingorder, int shopId = 6387655);
	}

	public class CreateShippingOrderRequest
	{
		public List<int>? InvoiceIds { get; set; }
		//public int? ServiceId { get; set; }
	}

	public class CalculateShippingFeeRequest
	{
		/// ID của gói dịch vụ (lấy được từ API available-services)
		//public int? ServiceId { get; set; }

		/// Giá trị của sản phẩm (VND). GHN tính tiền bảo hiểm dựa vào giá trị này
		public int InsuranceValue { get; set; }

		/// Mã giảm giá của GHN. Nếu không có, để rỗng hoặc null
		public string Coupon { get; set; }

		/// ID Phường/Xã người nhận
		public string ToWardCode { get; set; }

		/// ID Quận/Huyện người nhận
		public int ToDistrictId { get; set; }

		/// ID Quận/Huyện người gửi
		public int FromDistrictId { get; set; }

		/// Trọng lượng hàng hóa (gram). Mặc định: 500
		public int Weight { get; set; } = 500;

		/// Chiều dài (cm). Mặc định: 15
		public int Length { get; set; } = 15;

		/// Chiều rộng (cm). Mặc định: 15
		public int Width { get; set; } = 15;

		/// Chiều cao (cm). Mặc định: 15
		public int Height { get; set; } = 15;
	}
}
