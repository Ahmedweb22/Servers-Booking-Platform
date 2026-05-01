
namespace Shatbly.ViewModels
{
    public class AvailabilityVM
    {
        public int Id { get; set; }

        public int WorkerId { get; set; }

        [Display(Name = "Day")]
        public Shatbly.Models.DayOfWeek DayOfWeek { get; set; }

        [Display(Name = "Start Time")]
        public TimeSpan StartTime { get; set; }

        [Display(Name = "End Time")]
        public TimeSpan EndTime { get; set; }
    }
}
