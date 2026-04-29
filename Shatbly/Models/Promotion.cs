using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shatbly.Models
{
    public enum DiscountType
    {
        Percentage,
        FixedAmount
    }

    public class Promotion
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        [Range(0.01, double.MaxValue)]
        public decimal DiscountValue { get; set; } = 0;

        [Required]
        public DiscountType DiscountType { get; set; } 

        [Column(TypeName = "decimal(10,2)")]
        public decimal MinOrderValue { get; set; } = 0;

        //[Required]
        [DataType(DataType.DateTime)]
        public DateTime? StartDate { get; set; }

        //[Required]
        [DataType(DataType.DateTime)]
        public DateTime? EndDate { get; set; }

        public bool IsActive { get; set; } = true;

        public int? CategoryId { get; set; }
        [ValidateNever]
        [ForeignKey(nameof(CategoryId))]
        public ServiceCategory Category { get; set; }
        [ValidateNever]
        public ICollection<PromotionCode> PromotionCodes { get; set; } 
    }
}
