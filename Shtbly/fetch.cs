using System;
using System.Net.Http;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true };
        using var client = new HttpClient(handler);
        var html = await client.GetStringAsync("https://localhost:7198/Customer/BookingSystem/CreateBooking?workerId=1");
        Console.WriteLine(html.Contains("value=\"1\""));
        int index = html.IndexOf("name=\"WorkerId\"");
        Console.WriteLine(html.Substring(Math.Max(0, index - 50), 100));
    }
}
