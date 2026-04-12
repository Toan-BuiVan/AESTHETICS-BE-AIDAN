using Microsoft.AspNetCore.Mvc;
using Aesthetics.Data.AestheticsInterfaces;
using Aesthetics.Entities.Models.RequestModel;
using Microsoft.AspNetCore.Components.Web;
using Aesthetics.Data.RepositoryInterfaces;

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

		[HttpPost("updatecustomer")]
		public async Task<bool> UpdateCustomer([FromBody] RequestUpdateCustomer request)
		{
			return await _customerService.UpdateCustomer(request);
		}

		[HttpPost("getlistcustomer")]
		public async Task<IActionResult> getlistcustomer([FromBody] RequestCustomer request)
		{
			var result = await _customerService.GetListCustomer(request);
			return Ok(result);
		}
	}
}