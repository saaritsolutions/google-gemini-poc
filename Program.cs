using GoogleGeminiAgent.Configuration;
using GoogleGeminiAgent.Models;
using GoogleGeminiAgent.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GoogleGeminiAgent;

class Program
{
    private static async Task Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();
        
        using (var scope = host.Services.CreateScope())
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

            // Check if running in documentation mode
            if (args.Length > 0 && args[0] == "--mode" && args.Length > 1 && args[1] == "documentation")
            {
                await RunDocumentationMode(scope.ServiceProvider, args, logger);
                return;
            }

            // Default interactive chat mode
            await RunInteractiveChatMode(scope.ServiceProvider, logger);
        }
    }

    private static async Task RunDocumentationMode(IServiceProvider serviceProvider, string[] args, ILogger<Program> logger)
    {
        logger.LogInformation("Starting Documentation Generation Mode...");
        
        var orchestrator = serviceProvider.GetRequiredService<IDocumentationOrchestrator>();
        
        // Parse command line arguments
        var repoName = GetArgValue(args, "--repo") ?? Environment.GetEnvironmentVariable("REPO_NAME") ?? "";
        var prNumberStr = GetArgValue(args, "--pr-number") ?? Environment.GetEnvironmentVariable("PR_NUMBER") ?? "";
        
        if (string.IsNullOrEmpty(repoName) || !int.TryParse(prNumberStr, out var prNumber))
        {
            logger.LogError("Missing required arguments: --repo and --pr-number");
            Console.WriteLine("Usage: dotnet run -- --mode documentation --repo owner/repo --pr-number 123");
            return;
        }
        
        logger.LogInformation("Generating documentation for {Repository} PR #{PRNumber}", repoName, prNumber);
        
        var success = await orchestrator.ProcessPullRequestDocumentationAsync(repoName, prNumber);
        
        if (success)
        {
            logger.LogInformation("✅ Documentation generation completed successfully!");
            Console.WriteLine("✅ Documentation generated successfully!");
        }
        else
        {
            logger.LogError("❌ Documentation generation failed");
            Console.WriteLine("❌ Documentation generation failed. Check logs for details.");
        }
    }

    private static async Task RunInteractiveChatMode(IServiceProvider serviceProvider, ILogger<Program> logger)
    {
        var geminiService = serviceProvider.GetRequiredService<IGeminiService>();
        var conversationManager = serviceProvider.GetRequiredService<IConversationManager>();
        var debugService = serviceProvider.GetRequiredService<GeminiDebugService>();

        logger.LogInformation("Google Gemini Agent POC Starting...");

        // Run diagnostics first
        await debugService.RunDiagnosticsAsync();

        // Check if service is available
        var isAvailable = await geminiService.IsServiceAvailableAsync();
        if (!isAvailable)
        {
            logger.LogError("Gemini API is not available. Please check your API key and configuration.");
            Console.WriteLine("\n❌ API not available. Check the diagnostics above for details.");
            return;
        }

        logger.LogInformation("Gemini API is available. Starting interactive session...");

        // Create a conversation session
        var session = conversationManager.CreateSession("demo-user");
        logger.LogInformation("Created conversation session: {SessionId}", session.Id);

        // Interactive chat loop
        await RunInteractiveChat(geminiService, conversationManager, session.Id, logger);
    }

    private static string? GetArgValue(string[] args, string argName)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == argName)
                return args[i + 1];
        }
        return null;
    }

    private static async Task RunInteractiveChat(
        IGeminiService geminiService, 
        IConversationManager conversationManager, 
        string sessionId, 
        ILogger logger)
    {
        Console.WriteLine("\n🤖 Google Gemini Agent POC");
        Console.WriteLine(new string('=', 50));
        Console.WriteLine("Type 'quit' to exit, 'clear' to clear history, 'history' to show conversation history");
        Console.WriteLine(new string('=', 50));

        while (true)
        {
            Console.Write("\n💬 You: ");
            var input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
                continue;

            if (input.ToLower() == "quit")
            {
                Console.WriteLine("👋 Goodbye!");
                break;
            }

            if (input.ToLower() == "clear")
            {
                conversationManager.ClearHistory(sessionId);
                Console.WriteLine("✅ Conversation history cleared.");
                continue;
            }

            if (input.ToLower() == "history")
            {
                ShowConversationHistory(conversationManager, sessionId);
                continue;
            }

            // Add user message to conversation
            var userMessage = new ConversationMessage
            {
                Role = "user",
                Content = input,
                Timestamp = DateTime.UtcNow
            };
            conversationManager.AddMessage(sessionId, userMessage);

            // Get AI response
            Console.Write("🤖 Agent: ");
            try
            {
                var response = await geminiService.GenerateContentAsync(input, sessionId);

                if (response.IsSuccessful)
                {
                    Console.WriteLine(response.Content);

                    // Add AI response to conversation
                    var aiMessage = new ConversationMessage
                    {
                        Role = "model",
                        Content = response.Content,
                        Timestamp = DateTime.UtcNow
                    };
                    conversationManager.AddMessage(sessionId, aiMessage);
                }
                else
                {
                    Console.WriteLine($"❌ Error: {response.ErrorMessage}");
                    logger.LogError("Failed to get response: {Error}", response.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Unexpected error: {ex.Message}");
                logger.LogError(ex, "Unexpected error during chat");
            }
        }
    }

    private static void ShowConversationHistory(IConversationManager conversationManager, string sessionId)
    {
        var history = conversationManager.GetHistory(sessionId, 20);
        
        if (!history.Any())
        {
            Console.WriteLine("📝 No conversation history available.");
            return;
        }

        Console.WriteLine("\n📝 Conversation History:");
        Console.WriteLine(new string('-', 50));

        foreach (var message in history)
        {
            var icon = message.Role == "user" ? "💬" : "🤖";
            var role = message.Role == "user" ? "You" : "Agent";
            Console.WriteLine($"{icon} {role} ({message.Timestamp:HH:mm:ss}): {message.Content}");
        }

        Console.WriteLine(new string('-', 50));
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: true);
                config.AddUserSecrets<Program>(optional: true);
                config.AddEnvironmentVariables();
                config.AddCommandLine(args);
            })
            .ConfigureServices((context, services) =>
            {
                // Configuration
                services.Configure<GeminiConfiguration>(
                    context.Configuration.GetSection(GeminiConfiguration.SectionName));

                // HTTP Clients
                services.AddHttpClient<IGeminiService, GeminiService>();
                services.AddHttpClient<IGitHubService, GitHubService>();

                // Core Services
                services.AddSingleton<IConversationManager, ConversationManager>();
                services.AddScoped<IGeminiService, GeminiService>();
                services.AddScoped<GeminiDebugService>();

                // Documentation Services
                services.AddScoped<IDocumentationService, DocumentationService>();
                services.AddScoped<IGitHubService, GitHubService>();
                services.AddScoped<IDocumentationOrchestrator, DocumentationOrchestrator>();

                // Logging
                services.AddLogging(builder =>
                {
                    builder.AddConsole();
                    builder.SetMinimumLevel(LogLevel.Information);
                });
            });
}
