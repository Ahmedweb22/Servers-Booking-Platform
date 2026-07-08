using Hangfire;
using System.Linq.Expressions;

namespace Shatbly.Services.Hangfire
{
    public class HangfireJobScheduler : IBackgroundJobScheduler
    {
        public string Schedule(Expression<Func<Task>> methodCall, TimeSpan delay)
        => BackgroundJob.Schedule(methodCall, delay);

        public void Delete(string jobId)
            => BackgroundJob.Delete(jobId);
    }
}
