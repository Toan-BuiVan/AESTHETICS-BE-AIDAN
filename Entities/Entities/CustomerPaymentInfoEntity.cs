using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Aesthetics.Entities.Entities
{
    [Table("CustomerPaymentInfos")]
    public class CustomerPaymentInfoEntity : Aesthetics.Entities.BaseEntity.BaseEntity
    {
        /// <summary>Liên kết đến khách hàng</summary>
        [ForeignKey("Customer")]
        public int? CustomerId { get; set; }

        /// <summary>Số tài khoản ngân hàng khách hàng</summary>
        [MaxLength(50)]
        public string? BankAccountNumber { get; set; }

        /// <summary>Tên chủ tài khoản</summary>
        [MaxLength(200)]
        public string? BankAccountName { get; set; }

        /// <summary>Tên ngân hàng (BIDV, VCB, ACB, MB, VP, etc)</summary>
        [MaxLength(100)]
        public string? BankName { get; set; }

        /// <summary>Mã ngân hàng (BIDV, VCB, ACB)</summary>
        [MaxLength(10)]
        public string? BankCode { get; set; }

        /// <summary>Là tài khoản mặc định? (dùng cho hoàn tiền)</summary>
        public bool IsDefault { get; set; }

        /// <summary>Ngày tạo/cập nhật lần cuối</summary>
        public DateTime? LastModifiedDate { get; set; }

        // Navigation properties
        public virtual CustomerEntity? Customer { get; set; }
    }
}