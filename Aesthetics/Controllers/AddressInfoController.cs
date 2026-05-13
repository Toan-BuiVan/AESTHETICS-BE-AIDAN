using Aesthetics.Data.AestheticsInterfaces;
using Aesthetics.Entities.Entities;
using Aesthetics.Entities.Models.RequestModel.DHN;
using Aesthetics.Entities.Models.ResponseModel;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Aesthetics.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AddressInfoController : ControllerBase
    {
        private readonly IAddressInfoService _addressInfoService;

        public AddressInfoController(IAddressInfoService addressInfoService)
        {
            _addressInfoService = addressInfoService;
        }

        [HttpPost("createaddressinfo")]
        public async Task<IActionResult> CreateAddress([FromBody] CreateAddressInfoModel model)
        {
            var result = await _addressInfoService.CreateAddressAsync(model);
            return Ok(new { success = result });
        }

        [HttpPost("updateaddressinfo")]
        public async Task<IActionResult> UpdateAddress([FromBody] UpdateAddressInfoModel model)
        {
            var result = await _addressInfoService.UpdateAddressAsync(model);
            return Ok(new { success = result });
        }

        [HttpPost("deleteaddressinfo")]
        public async Task<IActionResult> DeleteAddress([FromBody] DeleteAddressInfoModel model)
        {
            var result = await _addressInfoService.DeleteAddressAsync(model.AddressId);
            return Ok(new { success = result });
        }

        [HttpPost("getlistaddressinfo")]
        public async Task<IActionResult> GetAddressesList([FromBody] GetAddressInfoModel model)
        {
            var result = await _addressInfoService.GetAddressesListAsync(model);
            return Ok(result);
        }
    }
}