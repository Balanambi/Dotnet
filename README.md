# Dotnet
Dotnet
using System;
using System.Diagnostics;
using System.Threading;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("Usage: MyApiManager.exe [start|stop]");
            return;
        }

        string command = args[0].ToLower();

        switch (command)
        {
            case "start":
                StartApi();
                break;
            case "stop":
                StopApi();
                break;
            default:
                Console.WriteLine("Invalid command. Use 'start' or 'stop'.");
                break;
        }
    }

    static void StartApi()
    {
        // Replace this with the path to your API project's DLL
        string apiPath = @"path\to\your-api.dll";

        var apiProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"\"{apiPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        Console.WriteLine("Starting API...");
        apiProcess.Start();

        // Wait for the API to start (adjust this as needed)
        Thread.Sleep(2000);

        Console.WriteLine("API started.");
    }

    static void StopApi()
    {
        var processes = Process.GetProcessesByName("dotnet");

        foreach (var process in processes)
        {
            if (process.MainModule.FileName.Contains("your-api.dll"))
            {
                Console.WriteLine($"Stopping API (Process ID: {process.Id})...");
                process.Kill();
                Console.WriteLine("API stopped.");
            }
        }
    }
}
