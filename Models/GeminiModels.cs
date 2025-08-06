using System.Text.Json.Serialization;

namespace GoogleGeminiAgent.Models;

/// <summary>
/// Request model for Gemini API content generation
/// </summary>
public class GenerateContentRequest
{
    [JsonPropertyName("contents")]
    public List<Content> Contents { get; set; } = new();
    
    [JsonPropertyName("generationConfig")]
    public GenerationConfig? GenerationConfig { get; set; }
    
    [JsonPropertyName("safetySettings")]
    public List<SafetySetting>? SafetySettings { get; set; }
}

/// <summary>
/// Content structure for messages
/// </summary>
public class Content
{
    [JsonPropertyName("parts")]
    public List<Part> Parts { get; set; } = new();
    
    [JsonPropertyName("role")]
    public string? Role { get; set; }
}

/// <summary>
/// Part of a content (text, image, etc.)
/// </summary>
public class Part
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }
    
    [JsonPropertyName("inlineData")]
    public InlineData? InlineData { get; set; }
}

/// <summary>
/// Inline data for images or other media
/// </summary>
public class InlineData
{
    [JsonPropertyName("mimeType")]
    public string MimeType { get; set; } = string.Empty;
    
    [JsonPropertyName("data")]
    public string Data { get; set; } = string.Empty;
}

/// <summary>
/// Generation configuration parameters
/// </summary>
public class GenerationConfig
{
    [JsonPropertyName("temperature")]
    public double? Temperature { get; set; }
    
    [JsonPropertyName("topK")]
    public int? TopK { get; set; }
    
    [JsonPropertyName("topP")]
    public double? TopP { get; set; }
    
    [JsonPropertyName("maxOutputTokens")]
    public int? MaxOutputTokens { get; set; }
    
    [JsonPropertyName("stopSequences")]
    public List<string>? StopSequences { get; set; }
}

/// <summary>
/// Safety setting for content filtering
/// </summary>
public class SafetySetting
{
    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;
    
    [JsonPropertyName("threshold")]
    public string Threshold { get; set; } = string.Empty;
}

/// <summary>
/// Response model from Gemini API
/// </summary>
public class GenerateContentResponse
{
    [JsonPropertyName("candidates")]
    public List<Candidate> Candidates { get; set; } = new();
    
    [JsonPropertyName("promptFeedback")]
    public PromptFeedback? PromptFeedback { get; set; }
}

/// <summary>
/// Candidate response from the model
/// </summary>
public class Candidate
{
    [JsonPropertyName("content")]
    public Content? Content { get; set; }
    
    [JsonPropertyName("finishReason")]
    public string? FinishReason { get; set; }
    
    [JsonPropertyName("index")]
    public int Index { get; set; }
    
    [JsonPropertyName("safetyRatings")]
    public List<SafetyRating>? SafetyRatings { get; set; }
}

/// <summary>
/// Prompt feedback information
/// </summary>
public class PromptFeedback
{
    [JsonPropertyName("safetyRatings")]
    public List<SafetyRating>? SafetyRatings { get; set; }
}

/// <summary>
/// Safety rating for content
/// </summary>
public class SafetyRating
{
    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;
    
    [JsonPropertyName("probability")]
    public string Probability { get; set; } = string.Empty;
}
