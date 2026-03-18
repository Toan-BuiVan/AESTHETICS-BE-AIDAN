using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Models.RequestModel
{
    public class RequestLogin
    {
		public string UserName { get; set; }

		public string Password { get; set; }
	}

	public class RequestLogout
	{
		public string? AccessToken { get; set; }
	}
}
