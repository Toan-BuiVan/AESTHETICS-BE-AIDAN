using Aesthetics.Data.AestheticsInterfaces;
using Aesthetics.Entities.Models.RequestModel;
using Aesthetics.Entities.Models.ResponseModel;
using ASP_NetCore_Aesthetics.Filter;
using Microsoft.AspNetCore.Mvc;

namespace Aesthetics.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PermissionController : ControllerBase
    {
        private readonly IPermissionService _permissionService;

        public PermissionController(IPermissionService permissionService)
        {
            _permissionService = permissionService;
        }

		//[ServiceFilter(typeof(Filter_CheckToken))]
		//[Filter_Authorization("getpermissionlist")]
		[HttpPost("getpermissionlist")]
        public async Task<IActionResult> GetPermissionList([FromBody] GetPermissionListRequest request)
        {
            try
            {
                var result = await _permissionService.GetPermissionListAsync(request);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
		[ServiceFilter(typeof(Filter_CheckToken))]
		[Filter_Authorization("updatepermission")]
		[HttpPost("updatepermission")]
        public async Task<IActionResult> UpdatePermission([FromBody] UpdatePermissionRequest request)
        {
            try
            {
                var result = await _permissionService.UpdatePermissionAsync(request);
                return Ok(new { success = result, message = result ? "Cập nhật quyền thành công" : "Cập nhật quyền thất bại" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}