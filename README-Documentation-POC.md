# 📚 Google Gemini Documentation Agent POC

This POC demonstrates two powerful capabilities:
1. **Interactive AI Chat Agent** using Google Gemini API
2. **Automated Code Documentation** with GitHub Actions integration

## 🎯 Features

### Interactive Chat Mode
- Real-time conversation with Google Gemini AI
- Conversation history management
- Debug diagnostics and error handling
- Clean, user-friendly interface

### Documentation Generation Mode
- **Automated PR Analysis**: Analyzes code changes in pull requests
- **AI-Generated Documentation**: Creates comprehensive documentation using Gemini AI
- **Multiple Output Formats**: README updates, file documentation, PR comments
- **GitHub Integration**: Posts documentation directly to pull requests
- **Smart Analysis**: Focuses on code files, ignores binary/generated files

## 🚀 Quick Start

### Prerequisites
- .NET 9.0 SDK
- Google Gemini API key
- GitHub token (for documentation features)

### Setup

1. **Clone and build**:
   ```bash
   git clone <repo-url>
   cd google-gemini-poc
   dotnet restore
   dotnet build
   ```

2. **Configure API keys**:
   ```bash
   # Add to appsettings.json or environment variables
   export GEMINI_API_KEY="your-gemini-api-key"
   export GITHUB_TOKEN="your-github-token"  # For documentation features
   ```

3. **Run Interactive Chat**:
   ```bash
   dotnet run
   ```

4. **Run Documentation Generation**:
   ```bash
   dotnet run -- --mode documentation --repo owner/repo --pr-number 123
   ```

## 🤖 Interactive Chat Usage

```
🤖 Google Gemini Agent POC
==================================================
Type 'quit' to exit, 'clear' to clear history, 'history' to show conversation history
==================================================

💬 You: Hello! Can you help me write a function to calculate fibonacci numbers?

🤖 Agent: I'd be happy to help you write a Fibonacci function! Here are a few different approaches...
```

Commands:
- `quit` - Exit the application
- `clear` - Clear conversation history
- `history` - Show conversation history

## 📖 Documentation Generation

### GitHub Actions Setup

The POC includes a complete GitHub Actions workflow for automated documentation:

```yaml
# .github/workflows/auto-documentation.yml
name: Auto Documentation with Gemini AI

on:
  pull_request:
    types: [opened, synchronize, ready_for_review]
    branches: [main, develop]
```

### What Gets Documented

- **Code Analysis**: Analyzes changed `.cs`, `.js`, `.ts`, `.py`, `.java`, `.cpp` files
- **AI Documentation**: Generates comprehensive documentation for each file
- **PR Summary**: Creates overall pull request summary
- **README Updates**: Automatically updates README for significant changes
- **File Documentation**: Creates individual `.md` files in `/docs` folder

### Documentation Output Example

```markdown
## 🤖 AI-Generated Documentation

I've analyzed the code changes in this PR and generated comprehensive documentation.

### 📊 Summary
This pull request introduces a new user authentication service that handles JWT token generation and validation...

### 📁 Files Documented
- **Services/AuthService.cs**: JWT token management and user authentication logic
- **Models/UserModel.cs**: User entity model with authentication properties
```

## 🏗️ Architecture

### Core Components

- **GeminiService**: Google Gemini API integration
- **ConversationManager**: Chat session and history management
- **DocumentationService**: Code analysis and documentation generation
- **GitHubService**: GitHub API integration for PR analysis
- **DocumentationOrchestrator**: Main workflow coordinator

### Request Flow

```
PR Created → GitHub Action → Fetch Changes → Analyze Code → Generate Docs → Post Comments
```

## ⚙️ Configuration

### appsettings.json
```json
{
  "Gemini": {
    "ApiKey": "your-api-key",
    "BaseUrl": "https://generativelanguage.googleapis.com/v1beta",
    "Model": "gemini-2.5-flash",
    "MaxTokens": 1000,
    "Temperature": 0.7
  }
}
```

### Environment Variables
```bash
GEMINI_API_KEY=your-gemini-api-key
GITHUB_TOKEN=your-github-token
```

## 🔧 Development

### Project Structure
```
├── Services/
│   ├── GeminiService.cs           # AI API integration
│   ├── ConversationManager.cs     # Chat management
│   ├── DocumentationService.cs    # Doc generation
│   ├── GitHubService.cs           # GitHub integration
│   └── DocumentationOrchestrator.cs # Main orchestrator
├── Models/
│   ├── GeminiModels.cs            # AI request/response models
│   ├── ConversationModels.cs      # Chat models
│   └── DocumentationModels.cs     # Documentation models
├── Configuration/
│   └── GeminiConfiguration.cs     # Config models
└── .github/workflows/
    └── auto-documentation.yml     # GitHub Actions workflow
```

### Adding New Features

1. **Extend Documentation Types**: Add new file types in `DocumentationService`
2. **Custom Prompts**: Modify prompts in `DocumentationService` for specific documentation styles
3. **New Output Formats**: Add new documentation formats in `DocumentationOrchestrator`

## 🛠️ GitHub Actions Secrets

Required secrets in your GitHub repository:

- `GEMINI_API_KEY`: Your Google Gemini API key
- `GITHUB_TOKEN`: Automatically provided by GitHub Actions

## 📝 Example Use Cases

### 1. Code Review Enhancement
Automatically generate detailed explanations of complex code changes for reviewers.

### 2. Onboarding Documentation
Create comprehensive documentation for new team members joining the project.

### 3. API Documentation
Generate detailed API documentation from controller and service changes.

### 4. Architecture Documentation
Maintain up-to-date architecture documentation as the codebase evolves.

## 🎭 Demo Scenarios

### Interactive Chat Demo
```bash
# Start the agent
dotnet run

# Try these prompts:
💬 "Explain dependency injection in .NET"
💬 "Write a REST API controller for user management"
💬 "How do I implement async/await properly?"
```

### Documentation Demo
```bash
# Simulate documentation generation
dotnet run -- --mode documentation --repo your-org/your-repo --pr-number 42
```

## 🔍 Troubleshooting

### Common Issues

1. **API Key Issues**: Ensure your Gemini API key is valid and has proper quotas
2. **GitHub Permissions**: Verify your GitHub token has repository access
3. **Model Availability**: The POC uses `gemini-2.5-flash` - ensure it's available in your region

### Debug Mode
The application includes comprehensive logging. Set log level to `Debug` for detailed output:

```json
{
  "Logging": {
    "LogLevel": {
      "GoogleGeminiAgent": "Debug"
    }
  }
}
```

## 🚀 Deployment

### Local Development
```bash
dotnet run
```

### Production Deployment
```bash
dotnet publish -c Release
./bin/Release/net9.0/GoogleGeminiAgent
```

### Docker Support (Future Enhancement)
```dockerfile
FROM mcr.microsoft.com/dotnet/runtime:9.0
# ... dockerfile content
```

## 🤝 Contributing

This is a POC demonstrating Google Gemini integration with automated documentation. Feel free to:

- Add new documentation templates
- Implement additional file type support
- Enhance the GitHub integration
- Improve the conversation management

## 📄 License

This POC is for demonstration purposes. Please ensure you comply with Google Gemini API terms of service and GitHub API guidelines.

---

**Built with ❤️ using Google Gemini AI, .NET 9.0, and GitHub Actions**
