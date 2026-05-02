using Aesthetics.Data.AestheticsInterfaces;
using Aesthetics.Data.AestheticsInterfaces.ICommonService;
using Aesthetics.Data.RepositoryInterfaces;
using Aesthetics.Entities.Entities;
using Aesthetics.Entities.Models.RequestModel;
using Aesthetics.Entities.Models.ResponseModel;
using ClosedXML.Excel;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
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

		public async Task<byte[]?> ExportToExcelAsync(exportproduct product)
		{
			try
			{
				_logger.LogInformation("Starting ExportToExcelAsync with XLWorkbook");

				Expression<Func<ProductEntity, bool>> predicate = x => x.DeleteStatus != true;

				if (product.ProductIds != null && product.ProductIds.Any())
				{
					predicate = predicate.And(x => product.ProductIds.Contains(x.Id));
					_logger.LogInformation("Exporting {Count} specific products by IDs", product.ProductIds.Count);
				}
				else
				{
					_logger.LogInformation("Exporting all active products");
				}

				var allProducts = await _productRepository.FindByPredicate(predicate);
				var allProductsList = allProducts.ToList();

				if (!allProductsList.Any())
				{
					_logger.LogWarning("No products found for export");
					return Array.Empty<byte>();
				}

				_logger.LogInformation("Found {Count} products to export", allProductsList.Count);

				var supplierIds = allProductsList
					.Where(x => x.SupplierId.HasValue)
					.Select(x => x.SupplierId.Value)
					.Distinct()
					.ToList();

				var suppliers = new Dictionary<int, SupplierEntity>();
				if (supplierIds.Any())
				{
					var suppliersList = await _supplierRepository.FindByPredicate(x => supplierIds.Contains(x.Id));
					suppliers = suppliersList.ToDictionary(x => x.Id, x => x);
					_logger.LogInformation("Loaded {Count} suppliers", suppliers.Count);
				}

				foreach (var prod in allProductsList)
				{
					if (prod.SupplierId.HasValue && suppliers.TryGetValue(prod.SupplierId.Value, out var supplier))
					{
						prod.Supplier = supplier;
					}
				}

				var finalResults = allProductsList
					.OrderBy(x => x.Id)
					.ToList();

				_logger.LogInformation("Creating Excel export with {Count} products", finalResults.Count);

				byte[] result = null;

				try
				{
					// ✅ Tạo workbook với SimpleMode để tránh lỗi
					var workbook = new XLWorkbook();
					_logger.LogInformation("Workbook created successfully");

					var worksheet = workbook.Worksheets.Add("Products");
					_logger.LogInformation("Worksheet 'Products' added successfully");

					// ✅ Headers - không formatting phức tạp
					worksheet.Cell(1, 1).Value = "Id";
					worksheet.Cell(1, 2).Value = "SupplierName";
					worksheet.Cell(1, 3).Value = "ProductName";
					worksheet.Cell(1, 4).Value = "Description";
					worksheet.Cell(1, 5).Value = "SellingPrice";
					worksheet.Cell(1, 6).Value = "Quantity";
					worksheet.Cell(1, 7).Value = "Unit";
					worksheet.Cell(1, 8).Value = "MinimumStock";
					worksheet.Cell(1, 9).Value = "CostPrice";

					_logger.LogInformation("Headers added successfully");

					// ✅ Simple formatting - chỉ Bold font
					for (int col = 1; col <= 9; col++)
					{
						worksheet.Cell(1, col).Style.Font.Bold = true;
					}

					_logger.LogInformation("Header formatting applied");

					// ✅ Data
					int dataRowCount = 0;
					for (int i = 0; i < finalResults.Count; i++)
					{
						try
						{
							var row = i + 2;
							var item = finalResults[i];

							worksheet.Cell(row, 1).Value = item.Id;
							worksheet.Cell(row, 2).Value = item.Supplier?.SupplierName ?? "";
							worksheet.Cell(row, 3).Value = item.ProductName ?? "";
							worksheet.Cell(row, 4).Value = item.Description ?? "";
							worksheet.Cell(row, 5).Value = item.SellingPrice;
							worksheet.Cell(row, 6).Value = item.Quantity;
							worksheet.Cell(row, 7).Value = item.Unit ?? "";
							worksheet.Cell(row, 8).Value = item.MinimumStock;
							worksheet.Cell(row, 9).Value = item.CostPrice;

							dataRowCount++;

							if (dataRowCount % 100 == 0)
							{
								_logger.LogInformation("Added {Count} rows", dataRowCount);
							}
						}
						catch (Exception rowEx)
						{
							_logger.LogError(rowEx, "Error adding row {RowIndex} for product {ProductId}",
								i, finalResults[i].Id);
							throw;
						}
					}

					_logger.LogInformation("Data added successfully. Total rows: {RowCount}", dataRowCount);

					// ✅ Auto-fit columns
					try
					{
						worksheet.Columns().AdjustToContents();
						_logger.LogInformation("Columns adjusted to contents");
					}
					catch (Exception adjEx)
					{
						_logger.LogWarning(adjEx, "Warning: Could not adjust columns, continuing anyway");
						// Continue anyway - không fail nếu adjust không được
					}

					// ✅ Save to memory stream
					using (var stream = new MemoryStream())
					{
						_logger.LogInformation("Starting to save workbook to stream");

						workbook.SaveAs(stream);
						_logger.LogInformation("Workbook saved to stream. Stream length: {Length}", stream.Length);

						result = stream.ToArray();
						_logger.LogInformation("Excel file converted to byte array. Size: {Size} bytes", result.Length);

						if (result.Length == 0)
						{
							_logger.LogError("ERROR: Result byte array is empty!");
							return null;
						}
					}

					// ✅ Dispose workbook
					workbook.Dispose();
					_logger.LogInformation("Excel workbook disposed successfully");

					return result;
				}
				catch (Exception xlEx)
				{
					_logger.LogError(xlEx, "Error creating workbook - Exception type: {ExceptionType}, Message: {Message}, StackTrace: {StackTrace}",
						xlEx.GetType().Name, xlEx.Message, xlEx.StackTrace);
					throw;
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Export Products to Excel exception - Exception type: {ExceptionType}, Message: {Message}",
					ex.GetType().Name, ex.Message);
				return null;
			}
		}
	}
}