using GoogleGeminiAgent.Models;
using GoogleGeminiAgent.Services;
using Microsoft.Extensions.Logging;
using System.Text;

namespace GoogleGeminiAgent.Services;

/// <summary>
/// Main orchestrator for automated documentation generation
/// </summary>
public class DocumentationOrchestrator : IDocumentationOrchestrator
{
    private readonly IDocumentationService _documentationService;
    private readonly IGitHubService _gitHubService;
    private readonly ILogger<DocumentationOrchestrator> _logger;

    public DocumentationOrchestrator(
        IDocumentationService documentationService,
        IGitHubService gitHubService,
        ILogger<DocumentationOrchestrator> logger)
    {
        _documentationService = documentationService;
        _gitHubService = gitHubService;
        _logger = logger;
    }

    public async Task<bool> ProcessPullRequestDocumentationAsync(string repoName, int prNumber)
    {
        try
        {
            _logger.LogInformation("Starting documentation generation for {Repository} PR #{PRNumber}", repoName, prNumber);

            // 1. Fetch pull request information
            var pullRequest = await _gitHubService.GetPullRequestAsync(repoName, prNumber);
            if (pullRequest == null)
            {
                _logger.LogError("Failed to fetch pull request information");
                return false;
            }

            // 2. Filter relevant files (code files only)
            var codeFiles = pullRequest.ChangedFiles.Where(IsCodeFile).ToList();
            if (!codeFiles.Any())
            {
                _logger.LogInformation("No code files found in PR, skipping documentation generation");
                return true;
            }

            _logger.LogInformation("Processing {CodeFileCount} code files", codeFiles.Count);

            // 3. Generate documentation
            var analysisRequest = new CodeAnalysisRequest
            {
                RepositoryName = repoName,
                PullRequestNumber = prNumber,
                ChangedFiles = codeFiles
            };

            var documentationResult = await _documentationService.GenerateCodeDocumentationAsync(analysisRequest);
            
            if (!documentationResult.IsSuccessful)
            {
                _logger.LogError("Documentation generation failed: {Error}", documentationResult.ErrorMessage);
                return false;
            }

            // 4. Generate outputs
            var outputs = await GenerateDocumentationOutputsAsync(documentationResult, repoName);

            // 5. Post PR comment with documentation
            var commentPosted = await _gitHubService.PostPullRequestCommentAsync(
                repoName, 
                prNumber, 
                outputs.PullRequestComment);

            if (!commentPosted)
            {
                _logger.LogWarning("Failed to post PR comment, but documentation was generated successfully");
            }

            // 6. Update README if significant changes
            if (ShouldUpdateReadme(documentationResult))
            {
                await UpdateRepositoryReadmeAsync(repoName, outputs.UpdatedReadme);
            }

            // 7. Create documentation files
            await CreateDocumentationFilesAsync(repoName, outputs.FileDocuments);

            _logger.LogInformation("Documentation generation completed successfully for PR #{PRNumber}", prNumber);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing pull request documentation");
            return false;
        }
    }

    private async Task<DocumentationOutput> GenerateDocumentationOutputsAsync(DocumentationResult docResult, string repoName)
    {
        var output = new DocumentationOutput();

        // Generate markdown report
        output.MarkdownReport = GenerateMarkdownReport(docResult);

        // Generate PR comment
        output.PullRequestComment = GeneratePullRequestComment(docResult);

        // Generate individual file documentation
        foreach (var fileDoc in docResult.FileDocumentations.Where(f => f.IsSuccessful))
        {
            var docFileName = $"docs/{Path.GetFileNameWithoutExtension(fileDoc.FilePath)}.md";
            output.FileDocuments[docFileName] = GenerateFileMarkdown(fileDoc);
        }

        // Get current README and generate updated version
        var currentReadme = await _gitHubService.GetFileContentAsync(repoName, "README.md");
        if (!string.IsNullOrEmpty(currentReadme))
        {
            output.UpdatedReadme = await _documentationService.GenerateReadmeUpdateAsync(currentReadme, docResult);
        }

        return output;
    }

    private string GenerateMarkdownReport(DocumentationResult docResult)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("# 📚 Automated Code Documentation Report");
        sb.AppendLine();
        sb.AppendLine($"**Generated:** {docResult.GeneratedAt:yyyy-MM-dd HH:mm:ss UTC}");
        sb.AppendLine();
        
        sb.AppendLine("## 🎯 Pull Request Summary");
        sb.AppendLine();
        sb.AppendLine(docResult.PullRequestSummary);
        sb.AppendLine();
        
        sb.AppendLine("## 📋 File Documentation");
        sb.AppendLine();
        
        foreach (var fileDoc in docResult.FileDocumentations.Where(f => f.IsSuccessful))
        {
            sb.AppendLine($"### {fileDoc.FilePath}");
            sb.AppendLine();
            sb.AppendLine(fileDoc.Documentation);
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private string GeneratePullRequestComment(DocumentationResult docResult)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("## 🤖 AI-Generated Documentation");
        sb.AppendLine();
        sb.AppendLine("I've analyzed the code changes in this PR and generated comprehensive documentation.");
        sb.AppendLine();
        
        sb.AppendLine("### 📊 Summary");
        sb.AppendLine(docResult.PullRequestSummary);
        sb.AppendLine();
        
        sb.AppendLine("### 📁 Files Documented");
        foreach (var fileDoc in docResult.FileDocumentations.Where(f => f.IsSuccessful))
        {
            sb.AppendLine($"- **{fileDoc.FilePath}**: {fileDoc.Summary}");
        }
        sb.AppendLine();
        
        sb.AppendLine("### 🔍 Quick Actions");
        sb.AppendLine("- [ ] Review the generated documentation for accuracy");
        sb.AppendLine("- [ ] Update any missing technical details");
        sb.AppendLine("- [ ] Verify code examples and usage instructions");
        sb.AppendLine();
        
        sb.AppendLine($"*Documentation generated by Google Gemini AI at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}*");

        return sb.ToString();
    }

    private string GenerateFileMarkdown(FileDocumentation fileDoc)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine($"# Documentation: {Path.GetFileName(fileDoc.FilePath)}");
        sb.AppendLine();
        sb.AppendLine($"**File Path:** `{fileDoc.FilePath}`");
        sb.AppendLine($"**Generated:** {fileDoc.GeneratedAt:yyyy-MM-dd HH:mm:ss UTC}");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine(fileDoc.Documentation);

        return sb.ToString();
    }

    private bool IsCodeFile(ChangedFile file)
    {
        var codeExtensions = new[] { ".cs", ".js", ".ts", ".py", ".java", ".cpp", ".h", ".go", ".rs", ".php", ".rb" };
        var extension = Path.GetExtension(file.Path).ToLower();
        return codeExtensions.Contains(extension) && 
               !file.Path.Contains("/bin/") && 
               !file.Path.Contains("/obj/") &&
               !file.Path.Contains("/node_modules/") &&
               !file.Path.Contains("/.git/");
    }

    private bool ShouldUpdateReadme(DocumentationResult docResult)
    {
        // Update README for significant changes (new files, major modifications)
        var significantChanges = docResult.FileDocumentations
            .Count(f => f.FilePath.Contains("Service") || f.FilePath.Contains("Controller") || f.FilePath.Contains("Model"));
        
        return significantChanges >= 2;
    }

    private async Task UpdateRepositoryReadmeAsync(string repoName, string updatedReadme)
    {
        if (string.IsNullOrEmpty(updatedReadme))
            return;

        await _gitHubService.UpdateFileAsync(
            repoName, 
            "README.md", 
            updatedReadme, 
            "📚 Update README with latest code documentation");
    }

    private async Task CreateDocumentationFilesAsync(string repoName, Dictionary<string, string> fileDocuments)
    {
        foreach (var (filePath, content) in fileDocuments)
        {
            await _gitHubService.UpdateFileAsync(
                repoName, 
                filePath, 
                content, 
                $"📝 Generate documentation for {filePath}");
        }
    }
}
