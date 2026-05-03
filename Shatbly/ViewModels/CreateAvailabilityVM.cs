
namespace Shatbly.ViewModels
{
    public class CreateAvailabilityVM : IValidatableObject
    {
        public int Id { get; set; }

        [Required]
        public int WorkerId { get; set; }

        [Required]
        [Display(Name = "Day")]
        public Shatbly.Models.DayOfWeek DayOfWeek { get; set; }

        [Required]
        [DataType(DataType.Time)]
        [Display(Name = "Start Time")]
        public TimeSpan StartTime { get; set; }

        [Required]
        [DataType(DataType.Time)]
        [Display(Name = "End Time")]
        public TimeSpan EndTime { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (EndTime <= StartTime)
            {
                yield return new ValidationResult(
                    "End time must be after start time.",
                    new[] { nameof(EndTime) });
            }
        }
    }
}