using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Aesthetics.Entities.Entities
{
    [Table("AddressesInfo")]
    public class AddressInfoEntity : Aesthetics.Entities.BaseEntity.BaseEntity
	{
        /// <summary>FK → Customers: liên kết khách hàng</summary>
        [ForeignKey("Customer")]
        public int? CustomerId { get; set; }

        /// <summary>ID Tỉnh/Thành phố</summary>
        public int? ProvinceId { get; set; }

        /// <summary>Tên Tỉnh/Thành phố</summary>
        [MaxLength(255)]
        public string? ProvinceName { get; set; }

        /// <summary>ID Quận/Huyện</summary>
        public int? DistrictId { get; set; }

        /// <summary>Tên Quận/Huyện</summary>
        [MaxLength(255)]
        public string? DistrictName { get; set; }

        /// <summary>Code Phường/Xã</summary>
        [MaxLength(50)]
        public string? WardCode { get; set; }

        /// <summary>Tên Phường/Xã</summary>
        [MaxLength(255)]
        public string? WardName { get; set; }

		/// <summary>Tên Phường/Xã</summary>

		public string? DetailAddress { get; set; }

		/// <summary>Là địa chỉ mặc định hay không</summary>
		public bool? IsDefault { get; set; }

        /// <summary>Ngày tạo</summary>
        public DateTime? CreatedAt { get; set; }

        /// <summary>Navigation property - liên kết tới Customer</summary>
        public virtual CustomerEntity Customer { get; set; }
    }
}