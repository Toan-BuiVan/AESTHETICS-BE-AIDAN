using Aesthetics.Data.AestheticsInterfaces;
using Aesthetics.Data.RepositoryInterfaces;
using Aesthetics.Entities.Models.RequestModel;
using ASP_NetCore_Aesthetics.Filter;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Mvc;

namespace Aesthetics.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class CustomerController : ControllerBase
	{
		private readonly ICustomerService _customerService;

		public CustomerController(ICustomerService customerService)
		{
			_customerService = customerService;
		}

		[ServiceFilter(typeof(Filter_CheckToken))]
		[Filter_Authorization("updatecustomer")]
		[HttpPost("updatecustomer")]
		public async Task<bool> UpdateCustomer([FromBody] RequestUpdateCustomer request)
		{
			return await _customerService.UpdateCustomer(request);
		}

		//[ServiceFilter(typeof(Filter_CheckToken))]
		//[Filter_Authorization("getlistcustomer")]
		[HttpPost("getlistcustomer")]
		public async Task<IActionResult> getlistcustomer([FromBody] RequestCustomer request)
		{
			var result = await _customerService.GetListCustomer(request);
			return Ok(result);
		}
	}
}