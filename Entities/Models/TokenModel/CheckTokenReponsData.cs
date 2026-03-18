using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.DTO.NetCore.TokenModel
{
	public class CheckTokenReponsData
	{
		public int ResponseCode { get; set; }
		public string ResposeMessage { get; set; }
		public string Token { get; set; }
		public string RefreshToken { get; set; }
	}

	public class CheckTokenRequestData
	{
		public string? AccessToken { get; set; }
		public string? RefreshToken { get; set; }
		public int UserID { get; set; }
		public string? DeviceName { get; set; }
	}
}
