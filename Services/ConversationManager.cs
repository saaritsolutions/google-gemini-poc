using GoogleGeminiAgent.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace GoogleGeminiAgent.Services;

/// <summary>
/// In-memory conversation manager for managing chat sessions
/// </summary>
public class ConversationManager : IConversationManager
{
    private readonly ConcurrentDictionary<string, ConversationSession> _sessions;
    private readonly ILogger<ConversationManager> _logger;

    public ConversationManager(ILogger<ConversationManager> logger)
    {
        _sessions = new ConcurrentDictionary<string, ConversationSession>();
        _logger = logger;
    }

    public ConversationSession CreateSession(string userId)
    {
        var session = new ConversationSession
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow,
            IsActive = true
        };

        _sessions.TryAdd(session.Id, session);
        _logger.LogInformation("Created new conversation session {SessionId} for user {UserId}", session.Id, userId);

        return session;
    }

    public ConversationSession? GetSession(string conversationId)
    {
        _sessions.TryGetValue(conversationId, out var session);
        return session;
    }

    public void AddMessage(string conversationId, ConversationMessage message)
    {
        if (_sessions.TryGetValue(conversationId, out var session))
        {
            session.Messages.Add(message);
            session.LastUpdated = DateTime.UtcNow;

            // Keep only the last 100 messages to manage memory
            if (session.Messages.Count > 100)
            {
                var messagesToRemove = session.Messages.Count - 100;
                session.Messages.RemoveRange(0, messagesToRemove);
                _logger.LogDebug("Removed {Count} old messages from session {SessionId}", messagesToRemove, conversationId);
            }

            _logger.LogDebug("Added message to session {SessionId}: {Role} - {Content}", 
                conversationId, message.Role, message.Content.Substring(0, Math.Min(message.Content.Length, 50)));
        }
        else
        {
            _logger.LogWarning("Attempted to add message to non-existent session {SessionId}", conversationId);
        }
    }

    public List<ConversationMessage> GetHistory(string conversationId, int maxMessages = 10)
    {
        if (_sessions.TryGetValue(conversationId, out var session))
        {
            return session.Messages
                .OrderBy(m => m.Timestamp)
                .TakeLast(maxMessages)
                .ToList();
        }

        return new List<ConversationMessage>();
    }

    public void ClearHistory(string conversationId)
    {
        if (_sessions.TryGetValue(conversationId, out var session))
        {
            session.Messages.Clear();
            session.LastUpdated = DateTime.UtcNow;
            _logger.LogInformation("Cleared history for session {SessionId}", conversationId);
        }
    }

    public void EndSession(string conversationId)
    {
        if (_sessions.TryGetValue(conversationId, out var session))
        {
            session.IsActive = false;
            session.LastUpdated = DateTime.UtcNow;
            _logger.LogInformation("Ended session {SessionId}", conversationId);
        }
    }

    public List<ConversationSession> GetUserSessions(string userId)
    {
        return _sessions.Values
            .Where(s => s.UserId == userId && s.IsActive)
            .OrderByDescending(s => s.LastUpdated)
            .ToList();
    }

    public void DeleteSession(string conversationId)
    {
        if (_sessions.TryRemove(conversationId, out var session))
        {
            _logger.LogInformation("Deleted conversation session {SessionId}", conversationId);
        }
        else
        {
            _logger.LogWarning("Attempted to delete non-existent session {SessionId}", conversationId);
        }
    }

    /// <summary>
    /// Clean up old inactive sessions (can be called periodically)
    /// </summary>
    public void CleanupOldSessions(TimeSpan maxAge)
    {
        var cutoffTime = DateTime.UtcNow - maxAge;
        var sessionsToRemove = _sessions
            .Where(kvp => !kvp.Value.IsActive && kvp.Value.LastUpdated < cutoffTime)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var sessionId in sessionsToRemove)
        {
            _sessions.TryRemove(sessionId, out _);
        }

        if (sessionsToRemove.Any())
        {
            _logger.LogInformation("Cleaned up {Count} old sessions", sessionsToRemove.Count);
        }
    }
}
