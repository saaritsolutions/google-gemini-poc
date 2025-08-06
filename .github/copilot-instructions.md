# Copilot Instructions for Google Gemini Agent POC

<!-- Use this file to provide workspace-specific custom instructions to Copilot. For more details, visit https://code.visualstudio.com/docs/copilot/copilot-customization#_use-a-githubcopilotinstructionsmd-file -->

## Project Overview
This is a .NET Core console application that demonstrates Google Gemini API integration for creating an AI agent with conversation handling capabilities.

## Development Guidelines

### Code Style
- Use modern C# features and patterns
- Implement async/await for API calls
- Follow dependency injection patterns
- Use proper exception handling and logging

### Architecture
- Separate concerns with service classes
- Use interfaces for testability
- Implement configuration management
- Use structured logging

### Google Gemini API Integration
- Use HttpClient with proper configuration
- Implement retry policies for API calls
- Handle rate limiting appropriately
- Secure API key management

### Agent Capabilities
- Conversation memory management
- Context-aware responses
- Function calling support
- Multi-turn conversations

### Testing
- Unit tests for business logic
- Integration tests for API calls
- Mock external dependencies
- Test conversation flows

## Key Components
- GeminiService: Core API integration
- ConversationManager: Handle chat sessions
- AgentConfiguration: Settings and API keys
- MessageProcessor: Process and format messages
