using Aesthetics.Entities.Models.RequestModel;
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
		Task<JsonDocument> ReturnShippingOrdersAsync(ReturnShippingOrderRequest request, int shopId = 6387655);	}

	public class CreateShippingOrderRequest
	{
		public List<int>? InvoiceIds { get; set; }
		//public int? ServiceId { get; set; }
	}
}
