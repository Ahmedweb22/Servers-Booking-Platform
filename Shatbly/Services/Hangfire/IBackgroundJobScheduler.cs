using System.Linq.Expressions;

namespace Shtbly.Services.Hangfire
{
    public interface IBackgroundJobScheduler
    {
        string Schedule(Expression<Func<Task>> methodCall, TimeSpan delay);
        void Delete(string jobId);
    }
}
