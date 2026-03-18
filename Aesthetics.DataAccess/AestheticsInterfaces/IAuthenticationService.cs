using Aesthetics.Entities.Models.RequestModel;
using Aesthetics.Entities.Models.ResponseModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Data.AestheticsInterfaces
{
    public interface IAuthenticationService
    {
        Task<UserLoginResponse> login(RequestLogin request);
		Task<bool> logout(RequestLogout request);
		Task<bool> logoutall(RequestLogout request);
	}
}
