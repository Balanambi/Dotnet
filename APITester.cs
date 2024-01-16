using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        string apiUrl = "https://your-api-endpoint.com/upload"; // Replace with your API endpoint
        string bearerToken = "yourBearerToken"; // Replace with your actual bearer token
        int numberOfFiles = 50; // Adjust the number of files to upload

        List<Task> tasks = new List<Task>();

        for (int i = 0; i < numberOfFiles; i++)
        {
            tasks.Add(UploadFile(apiUrl, bearerToken, $"path/to/file_{i + 1}.txt")); // Adjust the file path pattern
        }

        await Task.WhenAll(tasks);

        Console.WriteLine("Stress test completed.");
    }

    static async Task UploadFile(string apiUrl, string bearerToken, string filePath)
    {
        using (HttpClient client = new HttpClient())
        using (var formData = new MultipartFormDataContent())
        using (var fileStream = new FileStream(filePath, FileMode.Open))
        {
            formData.Add(new StreamContent(fileStream), "file", Path.GetFileName(filePath));

            // Add Authorization header with Bearer token
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

            try
            {
                HttpResponseMessage response = await client.PostAsync(apiUrl, formData);

                // Handle the response as needed (e.g., log, analyze, etc.)
                Console.WriteLine($"Status Code: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
