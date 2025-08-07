using GoogleGeminiAgent.Configuration;
using GoogleGeminiAgent.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace GoogleGeminiAgent.Services;

/// <summary>
/// Service for interacting with Google Gemini API with enhanced documentation generation capabilities
/// </summary>
public class GeminiService : IGeminiService
{
    private const string MediaTypeApplicationJson = "application/json";
    private const string UserAgentHeaderValue = "GoogleGeminiAgent/1.0";
    private const int MaxConversationHistoryMessages = 10;
    private const string ApiKeyMaskPlaceholder = "***";

    private readonly HttpClient _httpClient;
    private readonly GeminiConfiguration _config;
    private readonly ILogger<GeminiService> _logger;
    private readonly IConversationManager _conversationManager;

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public GeminiService(
        HttpClient httpClient,
        IOptions<GeminiConfiguration> config,
        ILogger<GeminiService> logger,
        IConversationManager conversationManager)
    {
        _httpClient = httpClient;
        _config = config.Value;
        _logger = logger;
        _conversationManager = conversationManager;
        
        ConfigureHttpClient();
    }

    private void ConfigureHttpClient()
    {
        _httpClient.BaseAddress = new Uri(_config.BaseUrl);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", UserAgentHeaderValue);
    }

    public async Task<AgentResponse> GenerateContentAsync(string prompt, string? conversationId = null)
    {
        try
        {
            _logger.LogInformation("Generating content for prompt: {Prompt}", prompt.Substring(0, Math.Min(prompt.Length, 50)));

            List<ConversationMessage> history = new();
            if (!string.IsNullOrEmpty(conversationId))
            {
                history = _conversationManager.GetHistory(conversationId);
            }

            return await GenerateContentWithContextAsync(prompt, history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating content");
            return CreateErrorResponse(ex.Message, conversationId);
        }
    }

    public async Task<AgentResponse> GenerateContentWithContextAsync(string prompt, List<ConversationMessage> history)
    {
        try
        {
            var request = BuildRequest(prompt, history);
            var fullUrl = BuildApiUrl(_config.Model);

            var json = JsonSerializer.Serialize(request, JsonSerializerOptions);

            _logger.LogDebug("Sending request to Gemini API: {Url}", MaskApiKey(fullUrl));

            using var httpClient = new HttpClient();
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, fullUrl);
            var content = new StringContent(json, null, MediaTypeApplicationJson);
            httpRequest.Content = content;
            
            var response = await httpClient.SendAsync(httpRequest);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Gemini API error: {StatusCode} - {Content}", response.StatusCode, errorContent);
                
                return CreateErrorResponse($"API Error: {response.StatusCode} - {errorContent}");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var geminiResponse = JsonSerializer.Deserialize<GenerateContentResponse>(responseJson, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            return ProcessResponse(geminiResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Gemini API");
            return CreateErrorResponse(ex.Message);
        }
    }

    public async Task<bool> IsServiceAvailableAsync()
    {
        // Try the configured model first, then fallback options that match Postman
        var modelsToTry = new[] 
        { 
            _config.Model, 
            "gemini-pro",
            "gemini-1.5-flash", 
            "gemini-1.5-pro"
        };

        foreach (var model in modelsToTry.Distinct())
        {
            try
            {
                _logger.LogInformation("Checking Gemini API availability with model: {Model}", model);
                _logger.LogDebug("Using API Key: {ApiKey}", _config.ApiKey.Substring(0, Math.Min(_config.ApiKey.Length, 10)) + "...");
                _logger.LogDebug("Base URL: {BaseUrl}", _config.BaseUrl);

                var testRequest = new GenerateContentRequest
                {
                    Contents = new List<Content>
                    {
                        new Content
                        {
                            Parts = new List<Part>
                            {
                                new Part { Text = "Hello" }
                            }
                        }
                    }
                };

                var url = model.StartsWith("models/") 
                    ? $"/{model}:generateContent?key={_config.ApiKey}"
                    : $"/models/{model}:generateContent?key={_config.ApiKey}";
                var fullUrl = $"{_config.BaseUrl}{url}";
                _logger.LogDebug("Full API URL: {Url}", url.Replace(_config.ApiKey, "***"));
                _logger.LogDebug("Complete URL: {FullUrl}", fullUrl.Replace(_config.ApiKey, "***"));
                
                var json = JsonSerializer.Serialize(testRequest, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                });
                _logger.LogDebug("Request JSON: {Json}", json);

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                // Try with full URL (like Postman) instead of relying on BaseAddress
                HttpResponseMessage response;
                try
                {
                    using var httpClient = new HttpClient();
                    response = await httpClient.PostAsync(fullUrl, content);
                    _logger.LogDebug("Used full URL approach");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Full URL failed: {Message}, trying relative URL", ex.Message);
                    response = await _httpClient.PostAsync(url, content);
                }

                _logger.LogInformation("API Response Status for {Model}: {StatusCode}", model, response.StatusCode);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("API Error Response for {Model}: {ErrorContent}", model, errorContent);
                }
                else
                {
                    _logger.LogInformation("✅ Gemini API is available with model: {Model}!", model);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "❌ Error checking model {Model}: {Message}", model, ex.Message);
            }
        }

        _logger.LogError("❌ All model attempts failed. Check your API key and network connection.");
        return false;
    }

    private GenerateContentRequest BuildRequest(string prompt, List<ConversationMessage> history)
    {
        var contents = new List<Content>();

        // Add conversation history
        foreach (var message in history.TakeLast(MaxConversationHistoryMessages))
        {
            contents.Add(new Content
            {
                Parts = new List<Part> { new Part { Text = message.Content } },
                Role = message.Role == "user" ? "user" : "model"
            });
        }

        // Add current prompt
        contents.Add(new Content
        {
            Parts = new List<Part> { new Part { Text = prompt } },
            Role = "user"
        });

        return new GenerateContentRequest
        {
            Contents = contents
            // Remove GenerationConfig and SafetySettings to match working Postman format
        };
    }

    private List<SafetySetting> GetDefaultSafetySettings()
    {
        return new List<SafetySetting>
        {
            new SafetySetting { Category = "HARM_CATEGORY_HARASSMENT", Threshold = "BLOCK_MEDIUM_AND_ABOVE" },
            new SafetySetting { Category = "HARM_CATEGORY_HATE_SPEECH", Threshold = "BLOCK_MEDIUM_AND_ABOVE" },
            new SafetySetting { Category = "HARM_CATEGORY_SEXUALLY_EXPLICIT", Threshold = "BLOCK_MEDIUM_AND_ABOVE" },
            new SafetySetting { Category = "HARM_CATEGORY_DANGEROUS_CONTENT", Threshold = "BLOCK_MEDIUM_AND_ABOVE" }
        };
    }

    private AgentResponse ProcessResponse(GenerateContentResponse? response)
    {
        if (response?.Candidates == null || !response.Candidates.Any())
        {
            return CreateErrorResponse("No response generated");
        }

        var candidate = response.Candidates.First();
        var content = candidate.Content?.Parts?.FirstOrDefault()?.Text ?? string.Empty;

        return new AgentResponse
        {
            Content = content,
            IsSuccessful = true,
            Timestamp = DateTime.UtcNow,
            MessageId = Guid.NewGuid().ToString(),
            Metadata = new Dictionary<string, object>
            {
                ["finishReason"] = candidate.FinishReason ?? "unknown",
                ["candidateIndex"] = candidate.Index
            }
        };
    }

    /// <summary>
    /// Builds the full API URL for the specified model
    /// </summary>
    /// <param name="model">The model name to use</param>
    /// <returns>Complete API URL with authentication</returns>
    private string BuildApiUrl(string model)
    {
        return $"{_config.BaseUrl}/models/{model}:generateContent?key={_config.ApiKey}";
    }

    /// <summary>
    /// Masks the API key in URLs for logging purposes
    /// </summary>
    /// <param name="url">URL containing API key</param>
    /// <returns>URL with masked API key</returns>
    private string MaskApiKey(string url)
    {
        return url.Replace(_config.ApiKey, ApiKeyMaskPlaceholder);
    }

    /// <summary>
    /// Creates a standardized error response
    /// </summary>
    /// <param name="errorMessage">Error message</param>
    /// <param name="conversationId">Optional conversation ID</param>
    /// <returns>AgentResponse with error details</returns>
    private static AgentResponse CreateErrorResponse(string errorMessage, string? conversationId = null)
    {
        return new AgentResponse
        {
            IsSuccessful = false,
            ErrorMessage = errorMessage,
            ConversationId = conversationId ?? string.Empty
        };
    }
}
