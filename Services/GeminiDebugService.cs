using GoogleGeminiAgent.Configuration;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace GoogleGeminiAgent.Services;

/// <summary>
/// Debug utility for testing Gemini API connectivity
/// </summary>
public class GeminiDebugService
{
    private readonly HttpClient _httpClient;
    private readonly GeminiConfiguration _config;

    public GeminiDebugService(HttpClient httpClient, IOptions<GeminiConfiguration> config)
    {
        _httpClient = httpClient;
        _config = config.Value;
    }

    public async Task RunDiagnosticsAsync()
    {
        Console.WriteLine("🔍 Running Gemini API Diagnostics...");
        Console.WriteLine(new string('=', 60));

        // 1. Check configuration
        await CheckConfigurationAsync();

        // 2. Test basic connectivity
        await TestConnectivityAsync();

        // 3. List available models
        await ListModelsAsync();

        // 4. Test with simple request
        await TestSimpleRequestAsync();

        Console.WriteLine(new string('=', 60));
        Console.WriteLine("✅ Diagnostics complete!");
    }

    private async Task CheckConfigurationAsync()
    {
        Console.WriteLine("\n📋 Configuration Check:");
        Console.WriteLine($"  API Key: {(_config.ApiKey?.Length > 0 ? $"{_config.ApiKey[..10]}..." : "❌ NOT SET")}");
        Console.WriteLine($"  Base URL: {_config.BaseUrl}");
        Console.WriteLine($"  Model: {_config.Model}");
        Console.WriteLine($"  Max Tokens: {_config.MaxTokens}");
        Console.WriteLine($"  Temperature: {_config.Temperature}");
    }

    private async Task TestConnectivityAsync()
    {
        Console.WriteLine("\n🌐 Connectivity Test:");
        try
        {
            var response = await _httpClient.GetAsync("https://generativelanguage.googleapis.com/");
            Console.WriteLine($"  Status: {response.StatusCode}");
            Console.WriteLine($"  Headers: {string.Join(", ", response.Headers.Select(h => h.Key))}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ❌ Error: {ex.Message}");
        }
    }

    private async Task ListModelsAsync()
    {
        Console.WriteLine("\n📝 Available Models:");
        try
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_config.ApiKey}";
            var response = await _httpClient.GetAsync(url);
            
            Console.WriteLine($"  Status: {response.StatusCode}");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(content);
                
                if (jsonDoc.RootElement.TryGetProperty("models", out var models))
                {
                    Console.WriteLine("  Available models:");
                    foreach (var model in models.EnumerateArray())
                    {
                        if (model.TryGetProperty("name", out var name))
                        {
                            Console.WriteLine($"    - {name.GetString()}");
                        }
                    }
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"  ❌ Error: {errorContent}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ❌ Exception: {ex.Message}");
        }
    }

    private async Task TestSimpleRequestAsync()
    {
        Console.WriteLine("\n🧪 Simple Request Test:");
        try
        {
            var request = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = "Say hello" }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(request, new JsonSerializerOptions { WriteIndented = true });
            Console.WriteLine($"  Request: {json}");

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent?key={_config.ApiKey}";
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content);

            Console.WriteLine($"  Status: {response.StatusCode}");
            var responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"  Response: {responseContent}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ❌ Exception: {ex.Message}");
        }
    }
}
