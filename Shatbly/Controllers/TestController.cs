using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Shatbly.Services.Hangfire.TestJob;

namespace Shatbly.Controllers
{
    public class TestController : Controller
    {
        public IActionResult FireAndForget()
        {
            BackgroundJob.Enqueue<TestJob>(job => job.RunAsync("Fire and forget job executed!"));
            return Content("Job scheduled! تحقق من hangfire-test.txt أو من /hangfire");
        }

        public IActionResult Delayed()
        {
            BackgroundJob.Schedule<TestJob>(
                job => job.RunAsync("Delayed job executed after 30 seconds!"),
                TimeSpan.FromSeconds(30));
            return Content("Job scheduled بعد 30 ثانية! افتح /hangfire وشوف Scheduled tab");
        }

        public IActionResult Recurring()
        {
            RecurringJob.AddOrUpdate<TestJob>(
                "test-recurring-job",
                job => job.RunAsync("Recurring job tick!"),
                Cron.Minutely);
            return Content("Recurring job كل دقيقة اتسجل! شوفه في /hangfire");
        }
    }
}
