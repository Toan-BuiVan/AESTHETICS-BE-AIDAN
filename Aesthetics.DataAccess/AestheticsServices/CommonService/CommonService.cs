using Aesthetics.Data.AestheticsInterfaces.ICommonService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Data.AestheticsServices.CommonService
{
    public class CommonService : ICommonService
	{
		private readonly ILogger<CommonService> _logger;
		private IConfiguration _configuration;
		public CommonService(ILogger<CommonService> logger, IConfiguration configuration)
		{
			_logger = logger;
			_configuration = configuration;
		}
		public async Task<string> BaseProcessingFunction64(string? servicesImage)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(servicesImage))
				{
					_logger.LogWarning("BaseProcessingFunction64: servicesImage is null or empty");
					return string.Empty;
				}

				var imagesFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Images");

				if (!Directory.Exists(imagesFolder))
				{
					Directory.CreateDirectory(imagesFolder);
					_logger.LogInformation("Created Images folder at path: {Path}", imagesFolder);
				}

				string imageName = Guid.NewGuid().ToString() + ".png";
				var imgPath = Path.Combine(imagesFolder, imageName);

				// Xử lý data:image/... base64
				if (servicesImage.Contains("data:image"))
				{
					servicesImage = servicesImage.Substring(servicesImage.LastIndexOf(',') + 1);
				}

				byte[] imageBytes;
				try
				{
					imageBytes = Convert.FromBase64String(servicesImage);

					if (imageBytes.Length == 0)
					{
						_logger.LogWarning("Decoded Base64 but byte array is empty");
						throw new Exception("Base64 rỗng sau decode.");
					}

					if (imageBytes.Length > 5 * 1024 * 1024) // 5MB
					{
						_logger.LogWarning("Image size too large: {Size} bytes", imageBytes.Length);
						throw new Exception("File quá lớn (>5MB).");
					}
				}
				catch (FormatException ex)
				{
					_logger.LogError(ex, "Base64 format invalid");
					throw new Exception("Base64 không hợp lệ hoặc corrupt.");
				}

				using (MemoryStream ms = new MemoryStream(imageBytes))
				using (System.Drawing.Image image = System.Drawing.Image.FromStream(ms, true))
				{
					try
					{
						image.Save(imgPath, System.Drawing.Imaging.ImageFormat.Png);
					}
					catch (UnauthorizedAccessException ex)
					{
						_logger.LogError(ex, "Access denied when saving file at path: {Path}", imgPath);
						throw new Exception("Quyền truy cập bị từ chối khi lưu file. Kiểm tra permissions hosting.");
					}
				}

				_logger.LogInformation("Image saved successfully: {ImageName}", imageName);
				return imageName;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in BaseProcessingFunction64");
				throw;
			}
		}

		public async Task<string> GenerateRefreshToken()
		{
			var randomNumber = new byte[64];
			using var rng = RandomNumberGenerator.Create();
			rng.GetBytes(randomNumber);
			return Convert.ToBase64String(randomNumber);
		}
	}
}
