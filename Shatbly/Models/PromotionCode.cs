using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shatbly.Models
{
    public class PromotionCode
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        public int MaxUses { get; set; } = 1;

        public int UsedCount { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        [Required]
        public int? PromotionId { get; set; }
        [ValidateNever]
        [ForeignKey(nameof(PromotionId))]
        public  Promotion Promotion { get; set; }
        [ValidateNever]
        public  ICollection<Booking> Bookings { get; set; }
    }
}
