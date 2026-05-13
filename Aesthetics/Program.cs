using Aesthetics.Data.AestheticsDbContext;
using Aesthetics.Data.AestheticsInterfaces;
using Aesthetics.Data.AestheticsInterfaces.AI;
using Aesthetics.Data.AestheticsInterfaces.EmailService;
using Aesthetics.Data.AestheticsInterfaces.GHN;
using Aesthetics.Data.AestheticsInterfaces.ICommonService;
using Aesthetics.Data.AestheticsInterfaces.TokenService;
using Aesthetics.Data.AestheticsServices;
using Aesthetics.Data.AestheticsServices.AI;
using Aesthetics.Data.AestheticsServices.CommonService;
using Aesthetics.Data.AestheticsServices.EmailService;
using Aesthetics.Data.AestheticsServices.GHN;
using Aesthetics.Data.AestheticsServices.TokenService;
using Aesthetics.Data.RepositoryInterfaces;
using Aesthetics.Data.RepositoryServices;
using Aesthetics.DTO.NetCore.DataObject.Model.Momo;
using ASP_NetCore_Aesthetics.Services.MomoServices;
using ASP_NetCore_Aesthetics.Services.VnPaySevices;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using Microsoft.Extensions.Caching.StackExchangeRedis;


var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
// ===== CORS Configuration =====
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins("http://localhost:3001", "http://localhost:3000", "https://yourdomain.com", "https://buitoan.somee.com")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

builder.Services.AddDbContext<AestheticsDbContext>(options =>
    options.UseSqlServer(configuration.GetConnectionString("aesthetics")));

builder.Services.AddHttpContextAccessor();

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ===== Repository Registrations =====
builder.Services.AddScoped<ISupplierRepository, SupplierRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IServiceRepository, ServiceRepository>();
builder.Services.AddScoped<IAppointmentRepositoty, AppointmentRepositoty>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IStaffRepository, StaffRepository>();
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IAppointmentAssignmentRepository, AppointmentAssignmentRepository>();
builder.Services.AddScoped<IAppointmentTimeLockRepository, AppointmentTimeLockRepository>();
builder.Services.AddScoped<IAuthenticationRepository, AuthenticationRepository>();
builder.Services.AddScoped<ICartProductRepository, CartProductRepository>();
builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<IClinicRepository, ClinicRepository>();
builder.Services.AddScoped<IClinicStaffRepository, ClinicStaffRepository>();
builder.Services.AddScoped<ICommentRepository, CommentRepository>();
builder.Services.AddScoped<ICustomerTreatmentPlansRepository, CustomerTreatmentPlansRepository>();
builder.Services.AddScoped<ICustomerTreatmentSessionsRepository, CustomerTreatmentSessionsRepository>();
builder.Services.AddScoped<IEquipmentRepository, EquipmentRepository>();
builder.Services.AddScoped<IInventoryAlertRepository, InventoryAlertRepository>();
builder.Services.AddScoped<IInvoiceDetailsRepository, InvoiceDetailsRepository>();
builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
builder.Services.AddScoped<IPerformanceLogRepository, PerformanceLogRepository>();
builder.Services.AddScoped<IServiceTypeRepository, ServiceTypeRepository>();
builder.Services.AddScoped<ISessionProductRepository, SessionProductRepository>();
builder.Services.AddScoped<IStaffShiftRepository, StaffShiftRepository>();
builder.Services.AddScoped<ITreatmentPlanRepository, TreatmentPlanRepository>();
builder.Services.AddScoped<ITreatmentSessionRepository, TreatmentSessionRepository>();
builder.Services.AddScoped<IVoucherRepository, VoucherRepository>();
builder.Services.AddScoped<IWalletRepository, WalletRepository>();
builder.Services.AddScoped<IAccountSessionsRepository, AccountSessionsRepository>();
builder.Services.AddScoped<ICustomerPaymentInfoRepository, CustomerPaymentInfoRepository>();
builder.Services.AddScoped<IRefundRepository, RefundRepository>();


// ===== Service Registrations =====
builder.Services.AddScoped<ICommonService, CommonService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<IAppointmentTimeLockService, AppointmentTimeLockService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IInventoryAlertService, InventoryAlertService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<IInventoryAlertRepository, InventoryAlertRepository>();
builder.Services.AddScoped<ISupplierSevice, SupplierSevice>();
builder.Services.AddScoped<ICartProductService, CartProductService>();
builder.Services.AddScoped<IClinicService, ClinicService>();
builder.Services.AddScoped<IClinicStaffService, ClinicStaffService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<ICustomerTreatmentPlansService, CustomerTreatmentPlansService>();
builder.Services.AddScoped<ICustomerTreatmentSessionsService, CustomerTreatmentSessionsService>();
builder.Services.AddScoped<IEquipmentService, EquipmentService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<IInvoiceDetailService, InvoiceDetailService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IServicesService, ServicesService>();
builder.Services.AddScoped<IServiceTypeService, ServiceTypeService>();
builder.Services.AddScoped<ISessionProductService, SessionProductService>();
builder.Services.AddScoped<IStaffShiftServices, StaffShiftServices>();
builder.Services.AddScoped<ISupplierSevice, SupplierSevice>();
builder.Services.AddScoped<ITreatmentPlanService, TreatmentPlanService>();
builder.Services.AddScoped<ITreatmentSessionService, TreatmentSessionService>();
builder.Services.AddScoped<IVoucherService, VoucherService>();
builder.Services.AddScoped<IWalletService, WalletService>();
builder.Services.AddScoped<IStaffService, StaffService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IAccountSessionsService, AccountSessionsService>();
builder.Services.AddScoped<IInvoicePaymentService, InvoicePaymentService>();
builder.Services.AddScoped<ICustomerPaymentInfoService, CustomerPaymentInfoService>();
builder.Services.AddScoped<IRefundServcie, RefundService>();
builder.Services.AddScoped<IGHNService, GHNService>();
builder.Services.AddScoped<IStatisticsService, StatisticsService>();

// Address Info
builder.Services.AddScoped<IAddressInfoRepository, AddressInfoRepository>();
builder.Services.AddScoped<IAddressInfoService, AddressInfoService>();


// ===== Payment Services =====
builder.Services.AddScoped<IVnPayService, VnPayService>();
builder.Services.AddScoped<IMomoService, MomoService>();
builder.Services.Configure<MomoOptionModel>(configuration.GetSection("MomoAPI"));

builder.Services.AddHttpClient<ILLMService, LLMService>()
	.ConfigureHttpClient(client =>
	{
		client.Timeout = TimeSpan.FromMinutes(2);
		client.DefaultRequestHeaders.Add("User-Agent", "Aesthetics-AI-Client");
	});

// ===== AI Services =====
builder.Services.AddScoped<IAIAppointmentService, AIAppointmentService>();
builder.Services.AddScoped<IAIAnalyticsService, AIAnalyticsService>();
builder.Services.AddScoped<IAICartService, AICartService>();
builder.Services.AddScoped<IAIFunctionCallingService, AIFunctionCallingService>();
builder.Services.AddDistributedMemoryCache();

// Background Service
builder.Services.AddHostedService<AppointmentReminderBackgroundService>();
builder.Services.AddHostedService<InventoryAlertBackgroundService>();

// Additional services you might need
builder.Services.AddScoped<IPasswordHasher<object>, PasswordHasher<object>>();
builder.Services.AddMemoryCache();
builder.Services.AddLogging();

// ===== REDIS DISTRIBUTED CACHE =====
var redisConnectionString = configuration["RedisCacheUrl"];
if (!string.IsNullOrEmpty(redisConnectionString))
{
	try
	{
		builder.Services.AddStackExchangeRedisCache(options =>
		{
			options.Configuration = redisConnectionString;
			options.InstanceName = "Aesthetics_";
		});
	}
	catch (Exception ex)
	{
		builder.Services.AddDistributedMemoryCache();
	}
}
else
{
	builder.Services.AddDistributedMemoryCache();
}

// If you need authentication/authorization - moved BEFORE builder.Build()
//builder.Services.AddAuthentication("Bearer")
//	.AddJwtBearer("Bearer", options =>
//	{
//		// JWT configuration
//	});

var app = builder.Build();

// ===== Middleware Configuration =====
app.UseCors("AllowAll");

if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseCors(builder => builder
	.AllowAnyOrigin()
	.AllowAnyMethod()
	.AllowAnyHeader()
	.WithExposedHeaders("New-AccessToken", "New-RefreshToken"));

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
