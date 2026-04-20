using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Models.RequestModel.DHN
{
	/// <summary>Model tạo địa chỉ</summary>
	public class CreateAddressInfoModel
	{
		public int CustomerId { get; set; }
		public int ProvinceId { get; set; }
		public string ProvinceName { get; set; }
		public int DistrictId { get; set; }
		public string DistrictName { get; set; }
		public string WardCode { get; set; }
		public string WardName { get; set; }
		public string DetailAddress { get; set; }
		public bool? IsDefault { get; set; }
	}

	public class DeleteAddressInfoModel
	{
		public int AddressId { get; set; }	}

	/// <summary>Model cập nhật địa chỉ</summary>
	public class UpdateAddressInfoModel
	{
		public int Id { get; set; }
		public int ProvinceId { get; set; }
		public string ProvinceName { get; set; }
		public int DistrictId { get; set; }
		public string DistrictName { get; set; }
		public string WardCode { get; set; }
		public string WardName { get; set; }
		public string DetailAddress { get; set; }
		public bool? IsDefault { get; set; }
	}

	public class GetAddressInfoModel : BaseSearchModel
	{
		public int? CustomerId { get; set; }
	}
}
