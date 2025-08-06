# Google Gemini Agent POC

A .NET Core console application demonstrating Google Gemini API integration for creating an AI agent with conversation handling capabilities.

## Features

- 🤖 Google Gemini API integration
- 💬 Interactive chat interface
- 📝 Conversation history management
- ⚙️ Configurable AI parameters
- 🔒 Safety settings and content filtering
- 🧠 Context-aware responses
- 📊 Structured logging

## Prerequisites

- .NET 9.0 SDK or later
- Google Gemini API key

## Setup

### 1. Get Google Gemini API Key

1. Go to [Google AI Studio](https://makersuite.google.com/app/apikey)
2. Create a new API key
3. Copy the API key for configuration

### 2. Configure the Application

You can configure the API key in several ways:

#### Option A: Update appsettings.json
```json
{
  "Gemini": {
    "ApiKey": "YOUR_ACTUAL_API_KEY_HERE"
  }
}
```

#### Option B: Use User Secrets (Recommended for development)
```bash
dotnet user-secrets set "Gemini:ApiKey" "YOUR_ACTUAL_API_KEY_HERE"
```

#### Option C: Environment Variable
```bash
export Gemini__ApiKey="YOUR_ACTUAL_API_KEY_HERE"
```

#### Option D: Command Line
```bash
dotnet run --Gemini:ApiKey="YOUR_ACTUAL_API_KEY_HERE"
```

### 3. Build and Run

```bash
# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run the application
dotnet run
```

## Usage

Once the application starts, you'll see an interactive chat interface:

```
🤖 Google Gemini Agent POC
==================================================
Type 'quit' to exit, 'clear' to clear history, 'history' to show conversation history
==================================================

💬 You: Hello! How are you today?
🤖 Agent: Hello! I'm doing well, thank you for asking. I'm here and ready to help you with any questions or tasks you might have. How are you doing today?
```

### Available Commands

- **Type any message**: Chat with the AI agent
- **`quit`**: Exit the application
- **`clear`**: Clear conversation history
- **`history`**: Show recent conversation history

## Configuration Options

| Setting | Description | Default |
|---------|-------------|---------|
| `Gemini:ApiKey` | Your Google Gemini API key | Required |
| `Gemini:Model` | Gemini model to use | `gemini-1.5-flash` |
| `Gemini:MaxTokens` | Maximum response tokens | `1000` |
| `Gemini:Temperature` | Response creativity (0.0-1.0) | `0.7` |
| `Gemini:TopP` | Nucleus sampling parameter | `0.9` |
| `Gemini:TopK` | Top-k sampling parameter | `40` |

## Project Structure

```
├── Configuration/
│   └── GeminiConfiguration.cs    # Configuration model
├── Models/
│   ├── ConversationModels.cs     # Chat session models
│   └── GeminiModels.cs          # API request/response models
├── Services/
│   ├── ConversationManager.cs    # Chat session management
│   ├── GeminiService.cs          # Gemini API integration
│   └── Interfaces.cs             # Service interfaces
├── Program.cs                    # Main application entry point
├── appsettings.json             # Configuration file
└── GoogleGeminiAgent.csproj     # Project file
```

## Key Components

### GeminiService
- Handles Google Gemini API communication
- Manages request/response serialization
- Implements retry logic and error handling
- Supports conversation context

### ConversationManager
- Manages chat sessions and message history
- Provides in-memory storage for conversations
- Supports multiple concurrent users
- Includes cleanup for old sessions

### Models
- **ConversationMessage**: Individual chat messages
- **ConversationSession**: Complete chat sessions
- **GenerateContentRequest/Response**: Gemini API models
- **AgentResponse**: Standardized response format

## Safety and Content Filtering

The application includes default safety settings to filter potentially harmful content:
- Harassment detection
- Hate speech filtering
- Sexually explicit content blocking
- Dangerous content filtering

## Development

### Adding New Features

1. **Custom Function Calling**: Extend the `GeminiService` to support function calling
2. **Persistent Storage**: Replace `ConversationManager` with database storage
3. **Web Interface**: Add ASP.NET Core controllers for web-based chat
4. **Authentication**: Add user authentication and authorization

### Testing

```bash
# Run tests (when added)
dotnet test

# Check code coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Troubleshooting

### Common Issues

1. **API Key Error**: Ensure your API key is correctly configured and has Gemini API access
2. **Network Issues**: Check internet connectivity and firewall settings
3. **Rate Limiting**: Implement retry logic if hitting API rate limits

### Logging

The application uses structured logging. Check console output for detailed information about:
- API requests and responses
- Conversation management
- Error details

## Next Steps

- [ ] Add persistent conversation storage
- [ ] Implement function calling capabilities
- [ ] Add web API endpoints
- [ ] Create unit and integration tests
- [ ] Add support for image inputs
- [ ] Implement streaming responses

## License

This project is a proof of concept for educational purposes.
