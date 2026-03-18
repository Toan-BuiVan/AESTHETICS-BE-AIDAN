using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Models.ResponseModel
{
    public class UserLoginResponse
    {
		public string Token { get; set; }
		public string RefreshToken { get; set; }
	}
}
