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

        /// <summary>Bài 6: Lấy dịch vụ theo khoảng giá</summary>
        Task<AIServicesByPriceResponse> GetServicesByPriceRangeAsync(decimal minPrice, decimal maxPrice, int? serviceTypeId = null);

        /// <summary>Bài 13: Lấy top sản phẩm bán chạy nhất</summary>
        Task<AITopProductsResponse> GetTopProductsAsync(int? serviceTypeId = null);

        /// <summary>Bài 14: Lấy thông tin chi tiết sản phẩm</summary>
        Task<AIProductDetailsResponse> GetProductDetailsAsync(int productId, int? diseaseId = null);

        /// <summary>Bài 15: Lấy sản phẩm theo khoảng giá</summary>
        Task<AIProductsByPriceResponse> GetProductsByPriceRangeAsync(decimal minPrice, decimal maxPrice, int? serviceTypeId = null);
    }
}