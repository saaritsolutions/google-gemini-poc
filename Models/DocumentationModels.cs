namespace GoogleGeminiAgent.Models;

/// <summary>
/// Request model for code analysis and documentation generation
/// </summary>
public class CodeAnalysisRequest
{
    public string RepositoryName { get; set; } = string.Empty;
    public int PullRequestNumber { get; set; }
    public string BaseSha { get; set; } = string.Empty;
    public string HeadSha { get; set; } = string.Empty;
    public List<ChangedFile> ChangedFiles { get; set; } = new();
}

/// <summary>
/// Represents a file that was changed in a pull request
/// </summary>
public class ChangedFile
{
    public string Path { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Diff { get; set; } = string.Empty;
    public ChangeType ChangeType { get; set; }
    public string Language { get; set; } = string.Empty;
    public int LinesAdded { get; set; }
    public int LinesRemoved { get; set; }
}

/// <summary>
/// Type of change made to a file
/// </summary>
public enum ChangeType
{
    Added,
    Modified,
    Deleted,
    Renamed,
    Copied
}

/// <summary>
/// Documentation generated for a specific file
/// </summary>
public class FileDocumentation
{
    public string FilePath { get; set; } = string.Empty;
    public string Documentation { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public bool IsSuccessful { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Complete documentation result for a pull request
/// </summary>
public class DocumentationResult
{
    public string PullRequestSummary { get; set; } = string.Empty;
    public List<FileDocumentation> FileDocumentations { get; set; } = new();
    public bool IsSuccessful { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// GitHub integration models
/// </summary>
public class GitHubPullRequest
{
    public int Number { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string BaseBranch { get; set; } = string.Empty;
    public string HeadBranch { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public List<ChangedFile> ChangedFiles { get; set; } = new();
}

/// <summary>
/// Documentation output formats
/// </summary>
public class DocumentationOutput
{
    public string MarkdownReport { get; set; } = string.Empty;
    public string UpdatedReadme { get; set; } = string.Empty;
    public Dictionary<string, string> FileDocuments { get; set; } = new();
    public string PullRequestComment { get; set; } = string.Empty;
}
