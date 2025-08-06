namespace GoogleGeminiAgent.Configuration;

/// <summary>
/// Configuration settings for the Google Gemini API
/// </summary>
public class GeminiConfiguration
{
    public const string SectionName = "Gemini";
    
    /// <summary>
    /// Google Gemini API Key
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
    
    /// <summary>
    /// Base URL for Gemini API
    /// </summary>
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta";
    
    /// <summary>
    /// Model to use (e.g., gemini-1.5-flash, gemini-1.5-pro)
    /// </summary>
    public string Model { get; set; } = "gemini-1.5-flash";
    
    /// <summary>
    /// Maximum tokens for response
    /// </summary>
    public int MaxTokens { get; set; } = 1000;
    
    /// <summary>
    /// Temperature for response generation (0.0 to 1.0)
    /// </summary>
    public double Temperature { get; set; } = 0.7;
    
    /// <summary>
    /// Top-p parameter for nucleus sampling
    /// </summary>
    public double TopP { get; set; } = 0.9;
    
    /// <summary>
    /// Top-k parameter for top-k sampling
    /// </summary>
    public int TopK { get; set; } = 40;
}
