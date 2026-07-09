namespace Shatbly.Services.Hangfire.TestJob
{
    public class TestJob
    {
        public Task RunAsync(string message)
        {
            var logPath = Path.Combine(Directory.GetCurrentDirectory(), "hangfire-test.txt");
            File.AppendAllText(logPath, $"{DateTime.Now}: {message}{Environment.NewLine}");
            return Task.CompletedTask;
        }
    }
}
