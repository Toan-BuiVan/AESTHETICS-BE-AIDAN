using Aesthetics.Entities.Models.ResponseModel.AI;
using System.Threading.Tasks;

namespace Aesthetics.Data.AestheticsInterfaces.AI
{
	public interface IAIAnalyticsService
	{
		/// <summary>Bài 4: Lấy dịch vụ nhiều người dùng nhất</summary>
		Task<AIMostUsedServiceResponse> GetMostUsedServiceAsync();

		/// <summary>Bài 5: Lấy bác sĩ tốt nhất của liệu trình</summary>
		Task<AIBestDoctorResponse> GetBestDoctorForTreatmentPlanAsync(int treatmentPlanId);

		/// <summary>Lấy bác sĩ tốt nhất (nhiều lịch đặt nhất) của một dịch vụ</summary>
		Task<AIBestDoctorResponse> GetBestDoctorForServiceAsync(int serviceId);

		/// <summary>Bài 6: Lấy dịch vụ theo khoảng giá</summary>
		Task<AIServicesByPriceResponse> GetServicesByPriceRangeAsync(decimal minPrice, decimal maxPrice, int? serviceTypeId = null);

		/// <summary>Bài 12: Tư vấn sản phẩm theo yêu cầu/từ khóa (da, mụn, lão hóa, v.v.)</summary>
		Task<AIProductsByPriceResponse> GetRecommendedProductsByCategoryAsync(string keyword);

		/// <summary>Bài 13: Lấy top sản phẩm bán chạy nhất (có thể giới hạn số lượng)</summary>
		Task<AITopProductsResponse> GetTopProductsAsync(int? limit = null, int? serviceTypeId = null);

		/// <summary>Bài 14: Lấy thông tin chi tiết sản phẩm</summary>
		Task<AIProductDetailsResponse> GetProductDetailsAsync(int productId, int? diseaseId = null);

		/// <summary>Bài 15: Lấy sản phẩm theo khoảng giá</summary>
		Task<AIProductsByPriceResponse> GetProductsByPriceRangeAsync(decimal minPrice, decimal maxPrice, int? serviceTypeId = null);

		/// <summary>Bài 16: Lấy thông tin các gói điều trị của liệu trình trẻ hóa da kèm các buổi điều trị</summary>
		Task<AITreatmentPackagesResponse> GetTreatmentPackagesByServiceNameAsync(string serviceName);
	}
}