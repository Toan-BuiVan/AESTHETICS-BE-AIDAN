using Aesthetics.Data.AestheticsInterfaces;
using Aesthetics.Data.AestheticsInterfaces.ICommonService;
using Aesthetics.Data.RepositoryInterfaces;
using Aesthetics.Entities.Entities;
using Aesthetics.Entities.Models.RequestModel;
using Aesthetics.Entities.Models.ResponseModel;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using XAct.Expressions;

namespace Aesthetics.Data.AestheticsServices
{
	public class ProductService : IProductService
	{
		private readonly ILogger<ProductService> _logger;
		private readonly IProductRepository _productRepository;
		private readonly ISupplierRepository _supplierRepository;
		private readonly IServiceTypeRepository _serviceTypeRepository;
		private readonly IInvoiceRepository _invoiceRepository;
		private readonly IInvoiceDetailsRepository _invoiceDetailsRepository;
		private ICommonService _commonService;

		public ProductService(ILogger<ProductService> logger
			, IProductRepository productRepository
			, ISupplierRepository supplierRepository
			, IServiceTypeRepository serviceTypeRepository
			, ICommonService commonService
			, IInvoiceRepository invoiceRepository
			, IInvoiceDetailsRepository invoiceDetailsRepository)
		{
			_logger = logger;
			_productRepository = productRepository;
			_supplierRepository = supplierRepository;
			_serviceTypeRepository = serviceTypeRepository;
			_commonService = commonService;
			_invoiceRepository = invoiceRepository;
			_invoiceDetailsRepository = invoiceDetailsRepository;
		}

		public async Task<bool> create(CreateProduct product)
		{
			try
			{
				// Validate ServiceTypeId exists if provided
				//if (product.ServiceTypeId != null)
				//{
				//	var serviceTypeExists = await _serviceTypeRepository.GetById(product.ServiceTypeId);
				//	if (serviceTypeExists == null)
				//	{
				//		_logger.LogWarning("Create Product failed: ServiceTypeId {ServiceTypeId} does not exist", product.ServiceTypeId);
				//		return false;
				//	}
				//}

				// Validate SupplierId exists if provided
				if (product.SupplierId != null)
				{
					var supplierExists = await _supplierRepository.GetById(product.SupplierId);
					if (supplierExists == null)
					{
						_logger.LogWarning("Create Product failed: SupplierId {SupplierId} does not exist", product.SupplierId);
						return false;
					}
				}

				string processedImages = product.ProductImages;
				if (!string.IsNullOrEmpty(product.ProductImages))
				{
					processedImages = await _commonService.BaseProcessingFunction64(product.ProductImages);
				}

				var newProduct = new ProductEntity
				{
					//ServiceTypeId = product.ServiceTypeId,
					SupplierId = product.SupplierId,
					ProductName = product.ProductName,
					Description = product.Description,
					SellingPrice = product.SellingPrice,
					Quantity = product.Quantity,
					Unit = product.Unit,
					MinimumStock = product.MinimumStock,
					ProductImages = processedImages,
					CostPrice = product.CostPrice
				};

				var productCreated = await _productRepository.CreateEntity(newProduct);
				if (!productCreated)
				{
					_logger.LogError("Failed to create product: {ProductName}", product.ProductName);
					return false;
				}

				// Tạo Invoice và InvoiceDetail với Type "NhapHang"
				var invoiceCreated = await CreatePurchaseInvoiceForProduct(newProduct, product);
				if (!invoiceCreated)
				{
					_logger.LogWarning("Failed to create purchase invoice for product: {ProductName}", product.ProductName);
					// Decide: return false if invoice is mandatory, or continue if optional
					// For now, we'll continue since product was created
				}

				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error creating product: {ProductName}", product.ProductName);
				return false;
			}
		}

		/// <summary>
		/// Tạo hóa đơn nhập hàng cho sản phẩm mới được tạo
		/// </summary>
		private async Task<bool> CreatePurchaseInvoiceForProduct(ProductEntity newProduct, CreateProduct product)
		{
			try
			{
				decimal totalMoney = (product.CostPrice * product.Quantity);

				// Tạo Invoice với Type = "NhapHang"
				var invoice = new InvoiceEntity
				{
					CustomerId = null, // Không có khách hàng cho hóa đơn nhập
					StaffId = 1, // Default staff hoặc có thể thêm vào CreateProduct request
					TotalMoney = totalMoney,
					PaidAmount = totalMoney, // Giả sử thanh toán ngay khi nhập
					OutstandingBalance = 0,
					DateCreated = DateTime.UtcNow,
					Status = "DaThanhToan", // Đã thanh toán khi nhập hàng
					Type = "NhapHang",
					OrderStatus = "DaGiao", // Hàng đã nhận
					DeleteStatus = false
				};

				var invoiceCreated = await _invoiceRepository.CreateEntity(invoice);
				if (!invoiceCreated)
				{
					_logger.LogError("Failed to create purchase invoice for product: {ProductName}", newProduct.ProductName);
					return false;
				}

				_logger.LogInformation("Created purchase invoice for product: ProductId {ProductId}, InvoiceId {InvoiceId}",
					newProduct.Id, invoice.Id);

				// Tạo InvoiceDetail
				var detail = new InvoiceDetailEntity
				{
					InvoiceId = invoice.Id,
					ProductId = newProduct.Id,
					ServiceId = null, // Chỉ có sản phẩm, không có dịch vụ
					Price = product.CostPrice,
					Quantity = product.Quantity,
					TotalMoney = totalMoney,
					Status = "DaThanhToan",
					Type = "NhapHang",
					DeleteStatus = false
				};

				var detailCreated = await _invoiceDetailsRepository.CreateEntity(detail);
				if (!detailCreated)
				{
					_logger.LogError("Failed to create purchase invoice detail for product: {ProductName}", newProduct.ProductName);
					return false;
				}

				_logger.LogInformation("Created purchase invoice detail: ProductId {ProductId}, Quantity {Quantity}, Total {Total}",
					newProduct.Id, product.Quantity, totalMoney);

				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error creating purchase invoice for product: {ProductName}", newProduct.ProductName);
				return false;
			}
		}

		public async Task<bool> delete(deleteProduct product)
		{
			try
			{
				_logger.LogInformation("Start deleting Product");
				var existingProduct = await _productRepository.GetById(product.Id);
				if (existingProduct == null)
				{
					_logger.LogWarning("Delete Product failed: Not found with Id {Id}", product.Id);
					return false;
				}
				var deleted = await _productRepository.DeleteRangeEntitiesStatus(existingProduct);
				if (!deleted)
				{
					_logger.LogError("Delete Product failed at repository level: Id {Id}", product.Id);
					return false;
				}
				_logger.LogInformation("Delete Product success: Id {Id}", product.Id);
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Delete Product exception: Id {Id}", product.Id);
				return false;
			}
		}

		public async Task<bool> update(updateProduct product)
		{
			try
			{
				var existingProduct = await _productRepository.GetById(product.Id);
				if (existingProduct == null)
				{
					_logger.LogWarning("Update Product failed: Not found with Id {Id}", product.Id);
					return false;
				}

				if (!string.IsNullOrWhiteSpace(product.ProductName)
					&& existingProduct.ProductName != product.ProductName)
				{
					var duplicate = await _productRepository.GetByName(product.ProductName);
					if (duplicate != null)
					{
						_logger.LogWarning("Update Product failed: Duplicate ProductName {ProductName}", product.ProductName);
						return false;
					}
					existingProduct.ProductName = product.ProductName.Trim();
				}

				//if (product.ServiceTypeId.HasValue)
				//{
				//	existingProduct.ServiceTypeId = product.ServiceTypeId.Value;
				//}

				if (product.SupplierId.HasValue)
				{
					existingProduct.SupplierId = product.SupplierId.Value;
				}

				if (!string.IsNullOrWhiteSpace(product.Description))
				{
					existingProduct.Description = product.Description.Trim();
				}

				if (product.SellingPrice.HasValue)
				{
					existingProduct.SellingPrice = product.SellingPrice.Value;
				}

				if (product.Quantity.HasValue)
				{
					existingProduct.Quantity = product.Quantity.Value;
				}

				if (!string.IsNullOrWhiteSpace(product.Unit))
				{
					existingProduct.Unit = product.Unit.Trim();
				}

				if (product.MinimumStock.HasValue)
				{
					existingProduct.MinimumStock = product.MinimumStock.Value;
				}

				if (!string.IsNullOrWhiteSpace(product.ProductImages)
					&& existingProduct.ProductImages != product.ProductImages)
				{
					var processedImages = await _commonService.BaseProcessingFunction64(product.ProductImages);
					existingProduct.ProductImages = processedImages;
				}

				if (product.CostPrice.HasValue)
				{
					existingProduct.CostPrice = product.CostPrice.Value;
				}

				var updated = await _productRepository.UpdateEntity(existingProduct);
				if (!updated)
				{
					_logger.LogError("Update Product failed at repository level: Id {Id}", product.Id);
					return false;
				}

				_logger.LogInformation("Update Product success: Id {Id}", product.Id);
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Update Product exception: Id {Id}", product.Id);
				return false;
			}
		}

		public async Task<BaseDataCollection<ProductListResponseModel>> getlist(getproduct product)
		{
			try
			{
				Expression<Func<ProductEntity, bool>> predicate = x => x.DeleteStatus != true;

				// Search by Id or ProductId (same field)
				if (product.Id.HasValue)
				{
					predicate = predicate.And(x => x.Id == product.Id.Value);
				}
				if (product.ProductId.HasValue)
				{
					predicate = predicate.And(x => x.Id == product.ProductId.Value);
				}

				// Search by ServiceTypeId
				//if (product.ServiceTypeId.HasValue)
				//{
				//	predicate = predicate.And(x => x.ServiceTypeId == product.ServiceTypeId.Value);
				//}

				// Search by SupplierId
				if (product.SupplierId.HasValue)
				{
					predicate = predicate.And(x => x.SupplierId == product.SupplierId.Value);
				}

				// Search by ProductName
				if (!string.IsNullOrWhiteSpace(product.ProductName))
				{
					var name = product.ProductName.ToLower();
					predicate = predicate.And(x => x.ProductName.ToLower().Contains(name));
				}

				// Get all matching products first
				var allMatching = await _productRepository.FindByPredicate(predicate);
				var allMatchingList = allMatching.ToList();

				// Get all unique ServiceTypeIds and SupplierIds
				//var serviceTypeIds = allMatchingList
				//	.Where(x => x.ServiceTypeId.HasValue)
				//	.Select(x => x.ServiceTypeId.Value)
				//	.Distinct()
				//	.ToList();

				var supplierIds = allMatchingList
					.Where(x => x.SupplierId.HasValue)
					.Select(x => x.SupplierId.Value)
					.Distinct()
					.ToList();

				// Load all service types and suppliers in batch
				//var serviceTypes = new Dictionary<int, ServiceTypeEntity>();
				var suppliers = new Dictionary<int, SupplierEntity>();

				//if (serviceTypeIds.Any())
				//{
				//	var serviceTypesList = await _serviceTypeRepository.FindByPredicate(x => serviceTypeIds.Contains(x.Id));
				//	serviceTypes = serviceTypesList.ToDictionary(x => x.Id, x => x);
				//}

				if (supplierIds.Any())
				{
					var suppliersList = await _supplierRepository.FindByPredicate(x => supplierIds.Contains(x.Id));
					suppliers = suppliersList.ToDictionary(x => x.Id, x => x);
				}

				// Apply navigation properties to products
				foreach (var productEntity in allMatchingList)
				{
					//if (productEntity.ServiceTypeId.HasValue && serviceTypes.ContainsKey(productEntity.ServiceTypeId.Value))
					//{
					//	productEntity.ServiceType = serviceTypes[productEntity.ServiceTypeId.Value];
					//}
					if (productEntity.SupplierId.HasValue && suppliers.ContainsKey(productEntity.SupplierId.Value))
					{
						productEntity.Supplier = suppliers[productEntity.SupplierId.Value];
					}
				}

				// Apply navigation property filters in memory
				var filteredResults = allMatchingList.AsQueryable();

				if (!string.IsNullOrWhiteSpace(product.SupplierName))
				{
					var supplierName = product.SupplierName.ToLower();
					filteredResults = filteredResults.Where(x => x.Supplier != null &&
						x.Supplier.SupplierName != null &&
						x.Supplier.SupplierName.ToLower().Contains(supplierName));
				}

				//if (!string.IsNullOrWhiteSpace(product.ServiceTypeName))
				//{
				//	var serviceTypeName = product.ServiceTypeName.ToLower();
				//	filteredResults = filteredResults.Where(x => x.ServiceType != null &&
				//		x.ServiceType.ServiceTypeName != null &&
				//		x.ServiceType.ServiceTypeName.ToLower().Contains(serviceTypeName));
				//}

				var finalResults = filteredResults.ToList();
				var totalCount = finalResults.Count;

				// Apply pagination and project to response model
				var pagedData = finalResults
					.OrderBy(x => x.ProductName)
					.Skip((product.PageNo - 1) * product.PageSize)
					.Take(product.PageSize)
					.Select(x => new ProductListResponseModel
					{
						Id = x.Id,
						//ServiceTypeName = x.ServiceType?.ServiceTypeName,
						SupplierName = x.Supplier?.SupplierName,
						ProductName = x.ProductName,
						Description = x.Description,
						SellingPrice = x.SellingPrice,
						Quantity = x.Quantity,
						Unit = x.Unit,
						ProductImages = x.ProductImages
					})
					.ToList();

				return new BaseDataCollection<ProductListResponseModel>(
					pagedData,
					totalCount,
					product.PageNo,
					product.PageSize
				);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GetList Product exception");
				return new BaseDataCollection<ProductListResponseModel>(
					null,
					0,
					product.PageNo,
					product.PageSize
				);
			}
		}

		public async Task<byte[]> ExportToExcelAsync(exportproduct product)
		{
			try
			{
				_logger.LogInformation("Start exporting Products to Excel");
				Expression<Func<ProductEntity, bool>> predicate = x => x.DeleteStatus != true;

				// Check if ProductIds list is provided and not empty
				if (product.ProductIds != null && product.ProductIds.Any())
				{
					// Filter by the provided list of product IDs
				 predicate = predicate.And(x => product.ProductIds.Contains(x.Id));
					_logger.LogInformation("Exporting {Count} specific products by IDs", product.ProductIds.Count);
				}
				else
				{
					// If no specific IDs provided, export all products (you might want to limit this)
					_logger.LogInformation("Exporting all active products");
				}

				// Get all matching products
				var allProducts = await _productRepository.FindByPredicate(predicate);
				var allProductsList = allProducts.ToList();

				// If no products found, return empty Excel
				if (!allProductsList.Any())
				{
					_logger.LogWarning("No products found for export");
					return CreateEmptyExcel();
				}

				// Get all unique ServiceTypeIds and SupplierIds
				//var serviceTypeIds = allProductsList
				//	.Where(x => x.ServiceTypeId.HasValue)
				//	.Select(x => x.ServiceTypeId.Value)
				//	.Distinct()
				//	.ToList();

				var supplierIds = allProductsList
					.Where(x => x.SupplierId.HasValue)
					.Select(x => x.SupplierId.Value)
					.Distinct()
					.ToList();

				// Load all service types and suppliers in batch
				var serviceTypes = new Dictionary<int, ServiceTypeEntity>();
				var suppliers = new Dictionary<int, SupplierEntity>();

				//if (serviceTypeIds.Any())
				//{
				//	var serviceTypesList = await _serviceTypeRepository.FindByPredicate(x => serviceTypeIds.Contains(x.Id));
				//	serviceTypes = serviceTypesList.ToDictionary(x => x.Id, x => x);
				//}

				if (supplierIds.Any())
				{
					var suppliersList = await _supplierRepository.FindByPredicate(x => supplierIds.Contains(x.Id));
					suppliers = suppliersList.ToDictionary(x => x.Id, x => x);
				}

				// Apply navigation properties to products
				foreach (var productEntity in allProductsList)
				{
					//if (productEntity.ServiceTypeId.HasValue && serviceTypes.ContainsKey(productEntity.ServiceTypeId.Value))
					//{
					//	productEntity.ServiceType = serviceTypes[productEntity.ServiceTypeId.Value];
					//}
					if (productEntity.SupplierId.HasValue && suppliers.ContainsKey(productEntity.SupplierId.Value))
					{
						productEntity.Supplier = suppliers[productEntity.SupplierId.Value];
					}
				}

				// Order results by ProductName
				var finalResults = allProductsList.OrderBy(x => x.ProductName).ToList();

				using (var package = new ExcelPackage())
				{
					var worksheet = package.Workbook.Worksheets.Add("Products");

					// Headers
					worksheet.Cells[1, 1].Value = "Id";
					worksheet.Cells[1, 2].Value = "ServiceTypeName";
					worksheet.Cells[1, 3].Value = "SupplierName";
					worksheet.Cells[1, 4].Value = "ProductName";
					worksheet.Cells[1, 5].Value = "Description";
					worksheet.Cells[1, 6].Value = "SellingPrice";
					worksheet.Cells[1, 7].Value = "Quantity";
					worksheet.Cells[1, 8].Value = "Unit";
					worksheet.Cells[1, 9].Value = "MinimumStock";
					worksheet.Cells[1, 10].Value = "ProductImages";
					worksheet.Cells[1, 11].Value = "CostPrice";
					worksheet.Cells[1, 12].Value = "Status";

					// Data rows
					for (int i = 0; i < finalResults.Count; i++)
					{
						var row = i + 2;
						worksheet.Cells[row, 1].Value = finalResults[i].Id;
						//worksheet.Cells[row, 2].Value = finalResults[i].ServiceType?.ServiceTypeName;
						worksheet.Cells[row, 3].Value = finalResults[i].Supplier?.SupplierName;
						worksheet.Cells[row, 4].Value = finalResults[i].ProductName;
						worksheet.Cells[row, 5].Value = finalResults[i].Description;
						worksheet.Cells[row, 6].Value = finalResults[i].SellingPrice;
						worksheet.Cells[row, 7].Value = finalResults[i].Quantity;
						worksheet.Cells[row, 8].Value = finalResults[i].Unit;
						worksheet.Cells[row, 9].Value = finalResults[i].MinimumStock;
						worksheet.Cells[row, 10].Value = finalResults[i].ProductImages;
						worksheet.Cells[row, 11].Value = finalResults[i].CostPrice;
					}

					// Format the worksheet
					worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

					// Add header formatting
					using (var range = worksheet.Cells[1, 1, 1, 12])
					{
						range.Style.Font.Bold = true;
						range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
						range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
						range.Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
						range.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
						range.Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
						range.Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
					}

					_logger.LogInformation("Successfully exported {Count} products to Excel", finalResults.Count);
					return package.GetAsByteArray();
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Export Products to Excel exception");
				return null;
			}
		}

		private byte[] CreateEmptyExcel()
		{
			using (var package = new ExcelPackage())
			{
				var worksheet = package.Workbook.Worksheets.Add("Products");

				// Headers
				worksheet.Cells[1, 1].Value = "Id";
				worksheet.Cells[1, 2].Value = "ServiceTypeName";
				worksheet.Cells[1, 3].Value = "SupplierName";
				worksheet.Cells[1, 4].Value = "ProductName";
				worksheet.Cells[1, 5].Value = "Description";
				worksheet.Cells[1, 6].Value = "SellingPrice";
				worksheet.Cells[1, 7].Value = "Quantity";
				worksheet.Cells[1, 8].Value = "Unit";
				worksheet.Cells[1, 9].Value = "MinimumStock";
				worksheet.Cells[1, 10].Value = "ProductImages";
				worksheet.Cells[1, 11].Value = "CostPrice";
				worksheet.Cells[1, 12].Value = "Status";

				// No data message
				worksheet.Cells[2, 1].Value = "No products found";
				worksheet.Cells["A2:L2"].Merge = true;
				worksheet.Cells[2, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
				worksheet.Cells[2, 1].Style.Font.Italic = true;

				// Format headers
				using (var range = worksheet.Cells[1, 1, 1, 12])
				{
					range.Style.Font.Bold = true;
					range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
					range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
				}

				worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
				return package.GetAsByteArray();
			}
		}
	}
}