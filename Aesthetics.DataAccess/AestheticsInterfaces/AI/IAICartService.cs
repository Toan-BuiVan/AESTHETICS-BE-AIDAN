using Aesthetics.Entities.Models.ResponseModel.AI;
using System.Threading.Tasks;

namespace Aesthetics.Data.AestheticsInterfaces.AI
{
    public interface IAICartService
    {
        /// <summary>Bài 16: Thêm sản phẩm vào giỏ hàng</summary>
        Task<AICartActionResponse> AddProductToCartAsync(int customerId, int productId, int quantity = 1);

        /// <summary>Bài 17: Xóa sản phẩm khỏi giỏ hàng</summary>
        Task<AICartActionResponse> RemoveProductFromCartAsync(int customerId, int productId);
    }
}