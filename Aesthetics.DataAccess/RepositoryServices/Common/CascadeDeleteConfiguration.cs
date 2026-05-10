using Aesthetics.Entities.Entities;
using System.Collections.Generic;

namespace Aesthetics.DataAccess.RepositoryServices.Common
{
    /// <summary>
    /// Định cấu hình cascading delete rules cho tất cả entities
    /// Chỉ định entity nào được cascading delete từ entity cha nào
    /// </summary>
    public static class CascadeDeleteConfiguration
    {
		/// <summary>
		/// Dictionary định nghĩa: Entity cha → HashSet các Entity con được phép cascade
		/// Nếu entity không nằm trong đây hoặc danh sách con rỗng, cascading sẽ dừng
		/// </summary>
		public static readonly Dictionary<string, HashSet<string>> AllowedCascadeRules = new()
		{
            // ServiceEntity → TreatmentPlans, Appointments, Comments (xóa dịch vụ → xóa liệu trình, lịch hẹn, bình luận)
            {
				nameof(ServiceEntity),
				new HashSet<string>
				{
					nameof(TreatmentPlanEntity),
					nameof(AppointmentEntity),
					nameof(CommentEntity)
				}
			},

            // TreatmentPlanEntity → TreatmentSessions, Appointments (xóa liệu trình → xóa buổi, lịch hẹn)
            {
				nameof(TreatmentPlanEntity),
				new HashSet<string>
				{
					nameof(TreatmentSessionEntity),
					nameof(AppointmentEntity)
				}
			},

            // TreatmentSessionEntity → SessionProducts, CustomerTreatmentSessions, Appointments (xóa buổi → xóa sản phẩm, buổi khách, lịch hẹn)
            {
				nameof(TreatmentSessionEntity),
				new HashSet<string>
				{
					nameof(SessionProductEntity),
					nameof(CustomerTreatmentSessionEntity),
					nameof(AppointmentEntity)
				}
			},

            // ClinicEntity → AppointmentTimeLocks, AppointmentAssignments, Appointments (xóa phòng → xóa lịch khóa, phân công, lịch hẹn)
            {
				nameof(ClinicEntity),
				new HashSet<string>
				{
					nameof(AppointmentTimeLockEntity),
					nameof(AppointmentAssignmentEntity),
					nameof(AppointmentEntity)
				}
			},

            // StaffEntity → AppointmentAssignments, Appointments, StaffShifts, PerformanceLogs (xóa nhân viên → xóa phân công, lịch hẹn, ca làm, nhật ký)
            {
				nameof(StaffEntity),
				new HashSet<string>
				{
					nameof(AppointmentAssignmentEntity),
					nameof(AppointmentEntity),
					nameof(StaffShiftEntity),
					nameof(PerformanceLogEntity)
				}
			},

            // ProductEntity → SessionProducts, CartProducts, Comments (xóa sản phẩm → xóa sản phẩm trong buổi/giỏ, bình luận)
            {
				nameof(ProductEntity),
				new HashSet<string>
				{
					nameof(SessionProductEntity),
					nameof(CartProductEntity),
					nameof(CommentEntity)
				}
			},

            // CustomerEntity → Appointments, CustomerTreatmentPlans, Comments, Wallets, Carts (xóa khách → xóa lịch hẹn, liệu trình, bình luận, ví, giỏ)
            {
				nameof(CustomerEntity),
				new HashSet<string>
				{
					nameof(AppointmentEntity),
					nameof(CustomerTreatmentPlanEntity),
					nameof(CommentEntity),
					nameof(WalletEntity),
					nameof(CartEntity)
				}
			},

            // CustomerTreatmentPlanEntity → CustomerTreatmentSessions, Appointments (xóa liệu trình khách → xóa buổi khách, lịch hẹn)
            {
				nameof(CustomerTreatmentPlanEntity),
				new HashSet<string>
				{
					nameof(CustomerTreatmentSessionEntity),
					nameof(AppointmentEntity)
				}
			},

            // SupplierEntity → Products, Comments (xóa nhà cung cấp → xóa sản phẩm, bình luận)
            {
				nameof(SupplierEntity),
				new HashSet<string>
				{
					nameof(ProductEntity),
					nameof(CommentEntity)
				}
			},

            // ServiceTypeEntity → Services, Clinics, Comments (xóa loại dịch vụ → xóa dịch vụ, phòng, bình luận)
            {
				nameof(ServiceTypeEntity),
				new HashSet<string>
				{
					nameof(ServiceEntity),
					nameof(ClinicEntity),
					nameof(CommentEntity)
				}
			},

            // CartEntity → CartProducts (xóa giỏ hàng → xóa sản phẩm trong giỏ)
            {
				nameof(CartEntity),
				new HashSet<string>
				{
					nameof(CartProductEntity)
				}
			},

            // VoucherEntity → Wallets (xóa voucher → xóa từ ví khách)
            {
				nameof(VoucherEntity),
				new HashSet<string>
				{
					nameof(WalletEntity)
				}
			},

            // AccountEntity → Permissions, AccountSessions (xóa tài khoản → xóa quyền, phiên đăng nhập)
            {
				nameof(AccountEntity),
				new HashSet<string>
				{
					nameof(PermissionEntity),
					nameof(AccountSessionEntity),
					nameof(CustomerEntity),
					nameof(StaffEntity)
				}
			},

            // FunctionEntity → Permissions (xóa chức năng → xóa quyền hạn)
            {
				nameof(FunctionEntity),
				new HashSet<string>
				{
					nameof(PermissionEntity)
				}
			}
		};

		/// <summary>
		/// Danh sách các entity KHÔNG được phép cascading delete bất kỳ
		/// Những entity này sẽ dừng cascading ngay lập tức
		/// </summary>
		public static readonly HashSet<string> NoCascadeTypes = new()
        {
            nameof(InvoiceEntity),
            nameof(InvoiceDetailEntity),
            nameof(RefundEntity),
        };

        /// <summary>
        /// Kiểm tra xem entity có được phép cascading từ parent không
        /// </summary>
        public static bool IsAllowedCascade(string parentEntityType, string childEntityType)
        {
            if (NoCascadeTypes.Contains(childEntityType))
                return false;

            if (AllowedCascadeRules.TryGetValue(parentEntityType, out var allowedChildren))
            {
                return allowedChildren.Contains(childEntityType);
            }

            return false;
        }

        /// <summary>
        /// Lấy danh sách child entities được phép cascading từ parent
        /// </summary>
        public static HashSet<string>? GetAllowedChildren(string parentEntityType)
        {
            return AllowedCascadeRules.TryGetValue(parentEntityType, out var children) ? children : null;
        }
    }
}