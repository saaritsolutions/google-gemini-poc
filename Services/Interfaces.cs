using GoogleGeminiAgent.Models;

namespace GoogleGeminiAgent.Services;

/// <summary>
/// Interface for Google Gemini API service
/// </summary>
public interface IGeminiService
{
    /// <summary>
    /// Generate content using Gemini API
    /// </summary>
    Task<AgentResponse> GenerateContentAsync(string prompt, string? conversationId = null);
    
    /// <summary>
    /// Generate content with conversation context
    /// </summary>
    Task<AgentResponse> GenerateContentWithContextAsync(string prompt, List<ConversationMessage> history);
    
    /// <summary>
    /// Check if the service is available
    /// </summary>
    Task<bool> IsServiceAvailableAsync();
}

/// <summary>
/// Interface for conversation management
/// </summary>
public interface IConversationManager
{
    /// <summary>
    /// Create a new conversation session
    /// </summary>
    ConversationSession CreateSession(string userId);
    
    /// <summary>
    /// Get conversation session by ID
    /// </summary>
    ConversationSession? GetSession(string conversationId);
    
    /// <summary>
    /// Add message to conversation
    /// </summary>
    void AddMessage(string conversationId, ConversationMessage message);
    
    /// <summary>
    /// Get conversation history
    /// </summary>
    List<ConversationMessage> GetHistory(string conversationId, int maxMessages = 10);
    
    /// <summary>
    /// Clear conversation history
    /// </summary>
    void ClearHistory(string conversationId);
    
    /// <summary>
    /// Delete conversation session
    /// </summary>
    void DeleteSession(string conversationId);
    
    /// <summary>
    /// Get all active sessions for a user
    /// </summary>
    List<ConversationSession> GetUserSessions(string userId);
}

/// <summary>
/// Interface for code documentation generation
/// </summary>
public interface IDocumentationService
{
    Task<DocumentationResult> GenerateCodeDocumentationAsync(CodeAnalysisRequest request);
    Task<string> GenerateReadmeUpdateAsync(string existingReadme, DocumentationResult docResult);
}

/// <summary>
/// Interface for GitHub API integration
/// </summary>
public interface IGitHubService
{
    Task<GitHubPullRequest?> GetPullRequestAsync(string repoName, int prNumber);
    Task<List<ChangedFile>> GetChangedFilesAsync(string repoName, int prNumber);
    Task<string> GetFileContentAsync(string repoName, string filePath, string gitRef = "HEAD");
    Task<bool> PostPullRequestCommentAsync(string repoName, int prNumber, string comment);
    Task<bool> UpdateFileAsync(string repoName, string filePath, string content, string message, string branch = "main");
}

/// <summary>
/// Interface for documentation orchestration
/// </summary>
public interface IDocumentationOrchestrator
{
    Task<bool> ProcessPullRequestDocumentationAsync(string repoName, int prNumber);
}
