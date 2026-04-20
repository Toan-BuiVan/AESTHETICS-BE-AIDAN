using Aesthetics.Entities.Entities;
using Aesthetics.Entities.Models.RequestModel;
using Aesthetics.Entities.Models.RequestModel.DHN;
using Aesthetics.Entities.Models.ResponseModel;
using System.Threading.Tasks;

namespace Aesthetics.Data.AestheticsInterfaces
{
    public interface IAddressInfoService
    {
        /// <summary>Tạo địa chỉ giao hàng mới</summary>
        Task<bool> CreateAddressAsync(CreateAddressInfoModel model);

        /// <summary>Cập nhật địa chỉ giao hàng</summary>
        Task<bool> UpdateAddressAsync(UpdateAddressInfoModel model);

        /// <summary>Xóa địa chỉ giao hàng</summary>
        Task<bool> DeleteAddressAsync(int addressId);

        /// <summary>Lấy danh sách địa chỉ của khách hàng</summary>
        Task<BaseDataCollection<AddressInfoEntity>> GetAddressesListAsync(GetAddressInfoModel model);

    }
}