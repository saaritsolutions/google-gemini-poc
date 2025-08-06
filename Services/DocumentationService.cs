using GoogleGeminiAgent.Models;
using GoogleGeminiAgent.Services;
using Microsoft.Extensions.Logging;
using System.Text;

namespace GoogleGeminiAgent.Services;

/// <summary>
/// Service for generating code documentation using Gemini AI
/// </summary>
public class DocumentationService : IDocumentationService
{
    private readonly IGeminiService _geminiService;
    private readonly ILogger<DocumentationService> _logger;

    public DocumentationService(IGeminiService geminiService, ILogger<DocumentationService> logger)
    {
        _geminiService = geminiService;
        _logger = logger;
    }

    public async Task<DocumentationResult> GenerateCodeDocumentationAsync(CodeAnalysisRequest request)
    {
        try
        {
            _logger.LogInformation("Generating documentation for {FileCount} changed files", request.ChangedFiles.Count);

            var documentationTasks = request.ChangedFiles.Select(async file =>
            {
                var fileDoc = await GenerateFileDocumentationAsync(file);
                return new FileDocumentation
                {
                    FilePath = file.Path,
                    Documentation = fileDoc.Content,
                    Summary = ExtractSummary(fileDoc.Content),
                    IsSuccessful = fileDoc.IsSuccessful,
                    ErrorMessage = fileDoc.ErrorMessage
                };
            });

            var fileDocs = await Task.WhenAll(documentationTasks);

            // Generate overall PR summary
            var prSummary = await GeneratePRSummaryAsync(request, fileDocs);

            return new DocumentationResult
            {
                PullRequestSummary = prSummary.Content,
                FileDocumentations = fileDocs.ToList(),
                IsSuccessful = prSummary.IsSuccessful && fileDocs.All(f => f.IsSuccessful),
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating code documentation");
            return new DocumentationResult
            {
                IsSuccessful = false,
                ErrorMessage = ex.Message,
                GeneratedAt = DateTime.UtcNow
            };
        }
    }

    private async Task<AgentResponse> GenerateFileDocumentationAsync(ChangedFile file)
    {
        var prompt = $"""
        As a senior software engineer and technical writer, analyze this code file and generate comprehensive documentation.

        **File:** {file.Path}
        **Change Type:** {file.ChangeType}

        **Current Code:**
        ```{GetFileExtension(file.Path)}
        {file.Content}
        ```

        {(string.IsNullOrEmpty(file.Diff) ? "" : $@"
        **Changes Made:**
        ```diff
        {file.Diff}
        ```")}

        Please provide:

        1. **File Overview**: What this file does and its purpose in the project
        2. **Key Components**: Classes, methods, functions with their responsibilities
        3. **Dependencies**: External libraries, frameworks, or modules used
        4. **Architecture Notes**: Design patterns, architectural decisions
        5. **Usage Examples**: How other parts of the system would use this code
        6. **Change Impact**: (if changes were made) What changed and why it matters
        7. **Technical Considerations**: Performance, security, maintainability notes

        Format as clear, professional documentation that would help a new developer understand the code quickly.
        """;

        return await _geminiService.GenerateContentAsync(prompt);
    }

    private async Task<AgentResponse> GeneratePRSummaryAsync(CodeAnalysisRequest request, FileDocumentation[] fileDocs)
    {
        var filesSummary = string.Join("\n", fileDocs.Select(f => 
            $"- **{f.FilePath}**: {f.Summary}"));

        var prompt = $"""
        As a technical lead, create a comprehensive pull request documentation summary.

        **Pull Request Context:**
        - Repository: {request.RepositoryName}
        - PR Number: #{request.PullRequestNumber}
        - Files Changed: {request.ChangedFiles.Count}

        **Files Modified:**
        {filesSummary}

        **Overall Changes:**
        {string.Join("\n", request.ChangedFiles.Select(f => $"- {f.Path} ({f.ChangeType})"))}

        Please provide:

        1. **🎯 Pull Request Summary**: High-level overview of what this PR accomplishes
        2. **🔧 Technical Changes**: Key technical modifications and their purpose
        3. **🏗️ Architecture Impact**: How these changes affect the overall system architecture
        4. **🔍 Code Quality**: Assessment of code quality, patterns used, best practices
        5. **⚡ Performance Considerations**: Any performance implications
        6. **🔒 Security Considerations**: Security aspects of the changes
        7. **🧪 Testing Recommendations**: Suggested testing strategies for these changes
        8. **📚 Documentation Updates**: What documentation should be updated
        9. **🚀 Deployment Notes**: Any special deployment or configuration considerations

        Format as a professional technical document suitable for code review and project documentation.
        """;

        return await _geminiService.GenerateContentAsync(prompt);
    }

    public async Task<string> GenerateReadmeUpdateAsync(string existingReadme, DocumentationResult docResult)
    {
        var prompt = $"""
        As a technical writer, update this README.md file to reflect the recent code changes.

        **Current README:**
        ```markdown
        {existingReadme}
        ```

        **Recent Changes Summary:**
        {docResult.PullRequestSummary}

        **Updated Files:**
        {string.Join("\n", docResult.FileDocumentations.Select(f => $"- {f.FilePath}"))}

        Please:
        1. Update relevant sections to reflect new functionality
        2. Add new sections if new major features were added
        3. Update installation/setup instructions if needed
        4. Update usage examples if APIs changed
        5. Maintain the existing style and structure
        6. Keep it concise but comprehensive

        Return only the updated README.md content in markdown format.
        """;

        var response = await _geminiService.GenerateContentAsync(prompt);
        return response.IsSuccessful ? response.Content : existingReadme;
    }

    private string ExtractSummary(string documentation)
    {
        // Extract first paragraph or overview section as summary
        var lines = documentation.Split('\n');
        var summary = lines.Take(3).FirstOrDefault(l => !string.IsNullOrWhiteSpace(l) && !l.StartsWith("#"));
        return summary?.Trim() ?? "Documentation generated for this file.";
    }

    private string GetFileExtension(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLower();
        return extension switch
        {
            ".cs" => "csharp",
            ".js" => "javascript",
            ".ts" => "typescript",
            ".py" => "python",
            ".java" => "java",
            ".cpp" or ".cc" => "cpp",
            ".h" => "c",
            ".json" => "json",
            ".yml" or ".yaml" => "yaml",
            _ => "text"
        };
    }
}
