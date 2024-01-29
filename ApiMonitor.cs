using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

class Program
{
    static async Task Main(string[] args)
    {
        var apiMonitor = new ApiMonitor();
        await apiMonitor.StartApiAsync();

        Console.WriteLine("Press any key to stop the monitoring.");
        Console.ReadKey();

        await apiMonitor.StopApiAsync();
    }
}

public class ApiMonitor
{
    private IHost apiHost;
    private Process apiProcess;

    public async Task StartApiAsync()
    {
        apiProcess = StartApiProcess();

        // Wait for API to start
        await WaitForApiToStartAsync();

        // Start monitoring loop
        _ = MonitorApiAsync();
    }

    public async Task StopApiAsync()
    {
        // Stop monitoring
        apiHost?.Dispose();

        // Stop API process
        if (apiProcess != null && !apiProcess.HasExited)
        {
            apiProcess.Kill();
            apiProcess.WaitForExit();
        }
    }

    private Process StartApiProcess()
    {
        var process = new Process
        {
           StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "MyApi.dll --urls=http://localhost:5000;https://localhost:5001", // Specify your desired ports
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = "path-to-your-api-folder" // Replace with the path to your API project folder
            }
        };

        process.Start();
        return process;
    }

    private async Task WaitForApiToStartAsync()
    {
        var httpClient = new HttpClient();
        var maxAttempts = 30;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                var response = await httpClient.GetAsync("http://localhost:5000/health");
                response.EnsureSuccessStatusCode();
                Console.WriteLine("API is running.");
                return;
            }
            catch (HttpRequestException)
            {
                Console.WriteLine($"Attempt {attempt}/{maxAttempts}: Waiting for API to start...");
                await Task.Delay(1000);
            }
        }

        throw new Exception("API did not start within the expected time.");
    }

    private async Task MonitorApiAsync()
    {
        while (true)
        {
            await Task.Delay(5000); // Adjust the monitoring interval as needed

            if (apiHost == null || apiHost?.Services == null)
            {
                Console.WriteLine("Restarting API...");
                apiHost?.Dispose();

                apiProcess = StartApiProcess();
                await WaitForApiToStartAsync();
            }
        }
    }
}
