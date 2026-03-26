using Aesthetics.Entities.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Xml.Linq;

namespace Aesthetics.Data.AestheticsDbContext
{
    public class AestheticsDbContext : DbContext
	{
		public AestheticsDbContext(DbContextOptions options) : base(options) 
		{

		}
		protected override void OnModelCreating(ModelBuilder builder)
		{
			base.OnModelCreating(builder);

			builder.Entity<AccountEntity>()
				.HasIndex(a => a.UserName)
				.IsUnique();

			builder.Entity<CustomerEntity>()
				.HasIndex(c => c.Phone)
				.IsUnique();
			builder.Entity<CustomerEntity>()
				.HasIndex(c => c.Email)
				.IsUnique();
			builder.Entity<CustomerEntity>()
				.HasIndex(c => c.IDCard)
				.IsUnique();

			builder.Entity<FunctionEntity>()
				.HasIndex(f => f.FunctionCode)
				.IsUnique();

			builder.Entity<VoucherEntity>()
				.HasIndex(v => v.Code)
				.IsUnique();

			builder.Entity<PermissionEntity>()
				.HasIndex(p => new { p.AccountId, p.FunctionId })
				.IsUnique();

			builder.Entity<ClinicStaffEntity>()
				.HasIndex(cs => new { cs.ClinicId, cs.StaffId })
				.IsUnique();

			//// Relationships
			//builder.Entity<CustomerEntity>()
			//	.HasOne(c => c.Account)
			//	.WithMany(a => a.Customer)
			//	.HasForeignKey(c => c.AccountId)
			//	.OnDelete(DeleteBehavior.Cascade);

			//builder.Entity<StaffEntity>()
			//	.HasOne(s => s.Account)
			//	.WithMany(a => a.Staffs)
			//	.HasForeignKey(s => s.AccountId)
			//	.OnDelete(DeleteBehavior.Cascade);

			builder.Entity<PermissionEntity>()
				.HasOne(p => p.Account)
				.WithMany(a => a.Permissions)
				.HasForeignKey(p => p.AccountId)
				.OnDelete(DeleteBehavior.Cascade);

			builder.Entity<PermissionEntity>()
				.HasOne(p => p.Function)
				.WithMany(f => f.Permissions)
				.HasForeignKey(p => p.FunctionId)
				.OnDelete(DeleteBehavior.Cascade);

			builder.Entity<ServiceEntity>()
				.HasOne(s => s.ServiceType)
				.WithMany(st => st.Services)
				.HasForeignKey(s => s.ServiceTypeId)
				.OnDelete(DeleteBehavior.Cascade);

			builder.Entity<ProductEntity>()
				.HasOne(p => p.ServiceType)
				.WithMany(st => st.Products)
				.HasForeignKey(p => p.ServiceTypeId)
				.OnDelete(DeleteBehavior.Cascade);

			builder.Entity<ProductEntity>()
				.HasOne(p => p.Supplier)
				.WithMany(s => s.Products)
				.HasForeignKey(p => p.SupplierId)
				.OnDelete(DeleteBehavior.SetNull);

			builder.Entity<CommentEntity>()
				.HasOne(c => c.Product)
				.WithMany(p => p.Comments)
				.HasForeignKey(c => c.ProductId)
				.OnDelete(DeleteBehavior.SetNull);

			builder.Entity<CommentEntity>()
				.HasOne(c => c.Service)
				.WithMany(s => s.Comments)
				.HasForeignKey(c => c.ServiceId)
				.OnDelete(DeleteBehavior.SetNull);

			builder.Entity<CommentEntity>()
				.HasOne(c => c.Customer)
				.WithMany(cu => cu.Comments)
				.HasForeignKey(c => c.CustomerId)
				.OnDelete(DeleteBehavior.Cascade);

			builder.Entity<AppointmentEntity>()
				.HasOne(a => a.Customer)
				.WithMany(c => c.Appointments)
				.HasForeignKey(a => a.CustomerId)
				.OnDelete(DeleteBehavior.Cascade);

			builder.Entity<AppointmentEntity>()
				.HasOne(a => a.Staff)
				.WithMany(s => s.Appointments)
				.HasForeignKey(a => a.StaffId)
				.OnDelete(DeleteBehavior.SetNull);

			builder.Entity<AppointmentEntity>()
				.HasOne(a => a.Service)
				.WithMany(s => s.Appointments)
				.HasForeignKey(a => a.ServiceId)
				.OnDelete(DeleteBehavior.SetNull);

			builder.Entity<AppointmentEntity>()
				.HasOne(a => a.CustomerTreatmentPlan)
				.WithMany(ctp => ctp.Appointments)
				.HasForeignKey(a => a.CustomerTreatmentPlanId)
				.OnDelete(DeleteBehavior.SetNull);

			builder.Entity<CartEntity>()
				.HasOne(c => c.Customer)
				.WithMany(cu => cu.Carts)
				.HasForeignKey(c => c.CustomerId)
				.OnDelete(DeleteBehavior.Cascade);

			//builder.Entity<CartProductEntity>()
			//	.HasOne(cp => cp.Cart)
			//	.WithMany(c => c.CartProducts)
			//	.HasForeignKey(cp => cp.CartId)
			//	.OnDelete(DeleteBehavior.Cascade);

			//builder.Entity<CartProductEntity>()
			//	.HasOne(cp => cp.Product)
			//	.WithMany(p => p.CartProducts)
			//	.HasForeignKey(cp => cp.ProductId)
			//	.OnDelete(DeleteBehavior.SetNull);

			//builder.Entity<CartProductEntity>()
			//	.HasOne(cp => cp.Service)
			//	.WithMany(s => s.CartProducts)
			//	.HasForeignKey(cp => cp.ServiceId)
			//	.OnDelete(DeleteBehavior.SetNull);

			builder.Entity<CartProductEntity>()
				.HasOne(cp => cp.TreatmentPlan)
				.WithMany(tp => tp.CartProducts)
				.HasForeignKey(cp => cp.TreatmentPlanId)
				.OnDelete(DeleteBehavior.SetNull);

			builder.Entity<WalletEntity>()
				.HasOne(w => w.Customer)
				.WithMany(c => c.Wallets)
				.HasForeignKey(w => w.CustomerId)
				.OnDelete(DeleteBehavior.Cascade);

			builder.Entity<WalletEntity>()
				.HasOne(w => w.Voucher)
				.WithMany(v => v.Wallets)
				.HasForeignKey(w => w.VoucherId)
				.OnDelete(DeleteBehavior.Cascade);

			builder.Entity<AccountSessionEntity>()
				.HasOne(asess => asess.Account)
				.WithMany(a => a.AccountSessions)
				.HasForeignKey(asess => asess.AccountId)
				.OnDelete(DeleteBehavior.Cascade);

			builder.Entity<InvoiceEntity>()
				.HasOne(i => i.Customer)
				.WithMany(c => c.Invoices)
				.HasForeignKey(i => i.CustomerId)
				.OnDelete(DeleteBehavior.SetNull);

			//builder.Entity<InvoiceEntity>()
			//	.HasOne(i => i.Staff)
			//	.WithMany(s => s.Invoices)
			//	.HasForeignKey(i => i.StaffId)
			//	.OnDelete(DeleteBehavior.SetNull);

			builder.Entity<InvoiceEntity>()
				.HasOne(i => i.Voucher)
				.WithMany(v => v.Invoices)
				.HasForeignKey(i => i.VoucherId)
				.OnDelete(DeleteBehavior.SetNull);

			builder.Entity<InvoiceDetailEntity>()
				.HasOne(id => id.Invoice)
				.WithMany(i => i.InvoiceDetails)
				.HasForeignKey(id => id.InvoiceId)
				.OnDelete(DeleteBehavior.Cascade);

			builder.Entity<InvoiceDetailEntity>()
				.HasOne(id => id.Product)
				.WithMany(p => p.InvoiceDetails)
				.HasForeignKey(id => id.ProductId)
				.OnDelete(DeleteBehavior.SetNull);

			builder.Entity<InvoiceDetailEntity>()
				.HasOne(id => id.Service)
				.WithMany(s => s.InvoiceDetails)
				.HasForeignKey(id => id.ServiceId)
				.OnDelete(DeleteBehavior.SetNull);

			builder.Entity<InvoiceDetailEntity>()
				.HasOne(id => id.Voucher)
				.WithMany(v => v.InvoiceDetails)
				.HasForeignKey(id => id.VoucherId)
				.OnDelete(DeleteBehavior.SetNull);

			builder.Entity<InvoiceDetailEntity>()
				.HasOne(id => id.TreatmentPlan)
				.WithMany(tp => tp.InvoiceDetails)
				.HasForeignKey(id => id.TreatmentPlanId)
				.OnDelete(DeleteBehavior.SetNull);

			builder.Entity<ClinicStaffEntity>()
				.HasOne(cs => cs.Clinic)
				.WithMany(c => c.ClinicStaffs)
				.HasForeignKey(cs => cs.ClinicId)
				.OnDelete(DeleteBehavior.Cascade);

			builder.Entity<ClinicStaffEntity>()
				.HasOne(cs => cs.Staff)
				.WithMany(s => s.ClinicStaffs)
				.HasForeignKey(cs => cs.StaffId)
				.OnDelete(DeleteBehavior.Cascade);

			builder.Entity<AppointmentAssignmentEntity>()
				.HasOne(aa => aa.Appointment)
				.WithMany(a => a.AppointmentAssignments)
				.HasForeignKey(aa => aa.AppointmentId)
				.OnDelete(DeleteBehavior.Cascade);

			builder.Entity<AppointmentAssignmentEntity>()
				.HasOne(aa => aa.Clinic)
				.WithMany(c => c.AppointmentAssignments)
				.HasForeignKey(aa => aa.ClinicId)
				.OnDelete(DeleteBehavior.SetNull);

			//builder.Entity<AppointmentAssignmentEntity>()
			//	.HasOne(aa => aa.ServiceType)
			//	.WithMany(st => st.AppointmentAssignments)
			//	.HasForeignKey(aa => aa.ServiceTypeId)
			//	.OnDelete(DeleteBehavior.SetNull);

			builder.Entity<AppointmentAssignmentEntity>()
				.HasOne(aa => aa.Staff)
				.WithMany(s => s.AppointmentAssignments)
				.HasForeignKey(aa => aa.StaffId)
				.OnDelete(DeleteBehavior.SetNull);

			builder.Entity<AppointmentAssignmentEntity>()
				.HasOne(aa => aa.Service)
				.WithMany(s => s.AppointmentAssignments)
				.HasForeignKey(aa => aa.ServiceId)
				.OnDelete(DeleteBehavior.SetNull);

			builder.Entity<AppointmentAssignmentEntity>()
				.HasOne(aa => aa.Equipment)
				.WithMany(e => e.AppointmentAssignments)
				.HasForeignKey(aa => aa.EquipmentId)
				.OnDelete(DeleteBehavior.SetNull);

			builder.Entity<StaffShiftEntity>()
				.HasOne(ss => ss.Staff)
				.WithMany(s => s.StaffShifts)
				.HasForeignKey(ss => ss.StaffId)
				.OnDelete(DeleteBehavior.Cascade);

			builder.Entity<PerformanceLogEntity>()
				.HasOne(pl => pl.Staff)
				.WithMany(s => s.PerformanceLogs)
				.HasForeignKey(pl => pl.StaffId)
				.OnDelete(DeleteBehavior.Cascade);

			builder.Entity<PerformanceLogEntity>()
				.HasOne(pl => pl.Invoice)
				.WithMany(i => i.PerformanceLogs)
				.HasForeignKey(pl => pl.InvoiceId)
				.OnDelete(DeleteBehavior.SetNull);

			builder.Entity<EquipmentEntity>()
				.HasOne(e => e.Clinic)
				.WithMany(c => c.Equipments)
				.HasForeignKey(e => e.ClinicId)
				.OnDelete(DeleteBehavior.Cascade);

			

			// TreatmentPlan, TreatmentSession, SessionProduct, CustomerTreatmentPlan, CustomerTreatmentSession
			builder.Entity<TreatmentPlanEntity>()
				.HasOne(tp => tp.Service)
				.WithMany(s => s.TreatmentPlans)
				.HasForeignKey(tp => tp.ServiceId)
				.OnDelete(DeleteBehavior.Cascade);

			builder.Entity<TreatmentSessionEntity>()
				.HasOne(ts => ts.TreatmentPlan)
				.WithMany(tp => tp.TreatmentSessions)
				.HasForeignKey(ts => ts.TreatmentPlanId)
				.OnDelete(DeleteBehavior.Cascade);

			builder.Entity<SessionProductEntity>()
				.HasOne(sp => sp.TreatmentSession)
				.WithMany(ts => ts.SessionProducts)
				.HasForeignKey(sp => sp.TreatmentSessionId)
				.OnDelete(DeleteBehavior.Cascade);

			builder.Entity<SessionProductEntity>()
				.HasOne(sp => sp.Product)
				.WithMany(p => p.SessionProducts)
				.HasForeignKey(sp => sp.ProductId)
				.OnDelete(DeleteBehavior.Cascade);

			builder.Entity<CustomerTreatmentPlanEntity>()
				.HasOne(ctp => ctp.Customer)
				.WithMany(c => c.CustomerTreatmentPlans)
				.HasForeignKey(ctp => ctp.CustomerId)
				.OnDelete(DeleteBehavior.Cascade);

			builder.Entity<CustomerTreatmentPlanEntity>()
				.HasOne(ctp => ctp.TreatmentPlan)
				.WithMany(tp => tp.CustomerTreatmentPlans)
				.HasForeignKey(ctp => ctp.TreatmentPlanId)
				.OnDelete(DeleteBehavior.Cascade);

			builder.Entity<CustomerTreatmentSessionEntity>()
				.HasOne(cts => cts.CustomerTreatmentPlan)
				.WithMany(ctp => ctp.CustomerTreatmentSessions)
				.HasForeignKey(cts => cts.CustomerTreatmentPlanId)
				.OnDelete(DeleteBehavior.Cascade);

			builder.Entity<CustomerTreatmentSessionEntity>()
				.HasOne(cts => cts.TreatmentSession)
				.WithMany(ts => ts.CustomerTreatmentSessions)
				.HasForeignKey(cts => cts.TreatmentSessionId)
				.OnDelete(DeleteBehavior.NoAction);

			//builder.Entity<CustomerTreatmentSessionEntity>()
			//	.HasOne(cts => cts.Appointment)
			//	.WithOne(a => a.CustomerTreatmentSession)
			//	.HasForeignKey<CustomerTreatmentSessionEntity>(cts => cts.AppointmentId)
			//	.OnDelete(DeleteBehavior.SetNull);

			//builder.Entity<CustomerTreatmentSessionEntity>()
			//	.HasOne(cts => cts.Staff)
			//	.WithMany(s => s.CustomerTreatmentSessions)
			//	.HasForeignKey(cts => cts.StaffId)
			//	.OnDelete(DeleteBehavior.SetNull);

			builder.Entity<InvoiceEntity>()
			.HasOne(i => i.Voucher)
			.WithMany(v => v.Invoices)
			.HasForeignKey(i => i.VoucherId)
			.OnDelete(DeleteBehavior.SetNull);

			// Thêm các configuration cho InvoiceEntity mà bị thiếu
			builder.Entity<InvoiceEntity>()
				.HasOne<TreatmentPlanEntity>()
				.WithMany(tp => tp.Invoices)
				.HasForeignKey(i => i.TreatmentPlanId)
				.OnDelete(DeleteBehavior.SetNull);

			builder.Entity<InvoiceEntity>()
				.HasOne<ServiceEntity>()
				.WithMany(s => s.Invoices)
				.HasForeignKey(i => i.ServiceId)
				.OnDelete(DeleteBehavior.SetNull);

			builder.Entity<InvoiceEntity>()
				.HasOne<ProductEntity>()
				.WithMany(p => p.Invoices)
				.HasForeignKey(i => i.ProductId)
				.OnDelete(DeleteBehavior.SetNull);

			builder.Entity<InvoiceEntity>()
				.HasOne(i => i.Staff)
				.WithMany(s => s.Invoices)
				.HasForeignKey(i => i.StaffId)
				.OnDelete(DeleteBehavior.SetNull);

			builder.Entity<InvoiceDetailEntity>()
				.HasOne(id => id.Invoice)
				.WithMany(i => i.InvoiceDetails)
				.HasForeignKey(id => id.InvoiceId)
				.OnDelete(DeleteBehavior.Cascade);
		}

		public DbSet<AccountEntity> Accounts { get; set; }
		public DbSet<CustomerEntity> Customers { get; set; }
		public DbSet<StaffEntity> Staffs { get; set; }
		public DbSet<ServiceTypeEntity> ServiceTypes { get; set; }
		public DbSet<FunctionEntity> Functions { get; set; }
		public DbSet<PermissionEntity> Permissions { get; set; }
		public DbSet<ServiceEntity> Services { get; set; }
		public DbSet<SupplierEntity> Suppliers { get; set; }
		public DbSet<ProductEntity> Products { get; set; }
		public DbSet<CommentEntity> Comments { get; set; }
		public DbSet<VoucherEntity> Vouchers { get; set; }
		public DbSet<AppointmentEntity> Appointments { get; set; }
		public DbSet<CartEntity> Carts { get; set; }
		public DbSet<CartProductEntity> CartProducts { get; set; }
		public DbSet<WalletEntity> Wallets { get; set; }
		public DbSet<AccountSessionEntity> AccountSessions { get; set; }
		public DbSet<InvoiceEntity> Invoices { get; set; }
		public DbSet<InvoiceDetailEntity> InvoiceDetails { get; set; }
		public DbSet<ClinicEntity> Clinics { get; set; }
		public DbSet<ClinicStaffEntity> ClinicStaffs { get; set; }
		public DbSet<AppointmentAssignmentEntity> AppointmentAssignments { get; set; }
		public DbSet<StaffShiftEntity> StaffShifts { get; set; }
		public DbSet<PerformanceLogEntity> PerformanceLogs { get; set; }
		public DbSet<EquipmentEntity> Equipments { get; set; }
		public DbSet<TreatmentPlanEntity> TreatmentPlans { get; set; }
		public DbSet<TreatmentSessionEntity> TreatmentSessions { get; set; }
		public DbSet<SessionProductEntity> SessionProducts { get; set; }
		public DbSet<CustomerTreatmentPlanEntity> CustomerTreatmentPlans { get; set; }
		public DbSet<CustomerTreatmentSessionEntity> CustomerTreatmentSessions { get; set; }
		public DbSet<InventoryAlertEntity> InventoryAlerts { get; set; }
	}
}
