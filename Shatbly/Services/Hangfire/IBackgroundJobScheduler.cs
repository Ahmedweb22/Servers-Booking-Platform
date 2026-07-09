using System.Linq.Expressions;

namespace Shatbly.Services.Hangfire
{
    public interface IBackgroundJobScheduler
    {
        string Schedule(Expression<Func<Task>> methodCall, TimeSpan delay);
        void Delete(string jobId);
    }
}
