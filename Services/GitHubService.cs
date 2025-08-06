using GoogleGeminiAgent.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace GoogleGeminiAgent.Services;

/// <summary>
/// Service for integrating with GitHub API for pull request documentation
/// </summary>
public class GitHubService : IGitHubService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GitHubService> _logger;
    private readonly string _githubToken;

    public GitHubService(HttpClient httpClient, ILogger<GitHubService> logger, IConfiguration config)
    {
        _httpClient = httpClient;
        _logger = logger;
        _githubToken = config["GITHUB_TOKEN"] ?? Environment.GetEnvironmentVariable("GITHUB_TOKEN") ?? "";
        
        ConfigureHttpClient();
    }

    private void ConfigureHttpClient()
    {
        _httpClient.BaseAddress = new Uri("https://api.github.com");
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_githubToken}");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "GoogleGeminiAgent-Documentation/1.0");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
    }

    public async Task<GitHubPullRequest?> GetPullRequestAsync(string repoName, int prNumber)
    {
        try
        {
            _logger.LogInformation("Fetching PR #{PRNumber} from {Repository}", prNumber, repoName);

            var response = await _httpClient.GetAsync($"/repos/{repoName}/pulls/{prNumber}");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to fetch PR: {StatusCode}", response.StatusCode);
                return null;
            }

            var jsonContent = await response.Content.ReadAsStringAsync();
            var prData = JsonSerializer.Deserialize<JsonElement>(jsonContent);

            var pr = new GitHubPullRequest
            {
                Number = prData.GetProperty("number").GetInt32(),
                Title = prData.GetProperty("title").GetString() ?? "",
                Body = prData.GetProperty("body").GetString() ?? "",
                BaseBranch = prData.GetProperty("base").GetProperty("ref").GetString() ?? "",
                HeadBranch = prData.GetProperty("head").GetProperty("ref").GetString() ?? "",
                Author = prData.GetProperty("user").GetProperty("login").GetString() ?? ""
            };

            // Get changed files
            pr.ChangedFiles = await GetChangedFilesAsync(repoName, prNumber);

            return pr;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching pull request");
            return null;
        }
    }

    public async Task<List<ChangedFile>> GetChangedFilesAsync(string repoName, int prNumber)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/repos/{repoName}/pulls/{prNumber}/files");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to fetch changed files: {StatusCode}", response.StatusCode);
                return new List<ChangedFile>();
            }

            var jsonContent = await response.Content.ReadAsStringAsync();
            var filesData = JsonSerializer.Deserialize<JsonElement[]>(jsonContent);

            var changedFiles = new List<ChangedFile>();

            foreach (var fileData in filesData ?? Array.Empty<JsonElement>())
            {
                var filename = fileData.GetProperty("filename").GetString() ?? "";
                var status = fileData.GetProperty("status").GetString() ?? "";
                var additions = fileData.GetProperty("additions").GetInt32();
                var deletions = fileData.GetProperty("deletions").GetInt32();
                var patch = fileData.TryGetProperty("patch", out var patchProp) ? patchProp.GetString() : "";

                // Get file content
                var content = await GetFileContentAsync(repoName, filename, "HEAD");

                var changeType = status switch
                {
                    "added" => ChangeType.Added,
                    "modified" => ChangeType.Modified,
                    "removed" => ChangeType.Deleted,
                    "renamed" => ChangeType.Renamed,
                    "copied" => ChangeType.Copied,
                    _ => ChangeType.Modified
                };

                changedFiles.Add(new ChangedFile
                {
                    Path = filename,
                    Content = content,
                    Diff = patch ?? "",
                    ChangeType = changeType,
                    Language = GetLanguageFromPath(filename),
                    LinesAdded = additions,
                    LinesRemoved = deletions
                });
            }

            return changedFiles;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching changed files");
            return new List<ChangedFile>();
        }
    }

    public async Task<string> GetFileContentAsync(string repoName, string filePath, string gitRef = "HEAD")
    {
        try
        {
            var response = await _httpClient.GetAsync($"/repos/{repoName}/contents/{filePath}?ref={gitRef}");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch file content for {FilePath}: {StatusCode}", filePath, response.StatusCode);
                return "";
            }

            var jsonContent = await response.Content.ReadAsStringAsync();
            var contentData = JsonSerializer.Deserialize<JsonElement>(jsonContent);

            if (contentData.TryGetProperty("content", out var contentProp))
            {
                var base64Content = contentProp.GetString() ?? "";
                var bytes = Convert.FromBase64String(base64Content.Replace("\n", ""));
                return Encoding.UTF8.GetString(bytes);
            }

            return "";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching file content for {FilePath}", filePath);
            return "";
        }
    }

    public async Task<bool> PostPullRequestCommentAsync(string repoName, int prNumber, string comment)
    {
        try
        {
            var payload = new
            {
                body = comment
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"/repos/{repoName}/issues/{prNumber}/comments", content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Posted documentation comment to PR #{PRNumber}", prNumber);
                return true;
            }
            else
            {
                _logger.LogError("Failed to post PR comment: {StatusCode}", response.StatusCode);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error posting pull request comment");
            return false;
        }
    }

    public async Task<bool> UpdateFileAsync(string repoName, string filePath, string content, string message, string branch = "main")
    {
        try
        {
            // Get current file SHA (if exists)
            var currentFile = await _httpClient.GetAsync($"/repos/{repoName}/contents/{filePath}?ref={branch}");
            string? sha = null;

            if (currentFile.IsSuccessStatusCode)
            {
                var currentContent = await currentFile.Content.ReadAsStringAsync();
                var currentData = JsonSerializer.Deserialize<JsonElement>(currentContent);
                sha = currentData.GetProperty("sha").GetString();
            }

            var payload = new
            {
                message = message,
                content = Convert.ToBase64String(Encoding.UTF8.GetBytes(content)),
                branch = branch,
                sha = sha
            };

            var json = JsonSerializer.Serialize(payload);
            var requestContent = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync($"/repos/{repoName}/contents/{filePath}", requestContent);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Updated file {FilePath} in repository", filePath);
                return true;
            }
            else
            {
                _logger.LogError("Failed to update file {FilePath}: {StatusCode}", filePath, response.StatusCode);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating file {FilePath}", filePath);
            return false;
        }
    }

    private string GetLanguageFromPath(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLower();
        return extension switch
        {
            ".cs" => "C#",
            ".js" => "JavaScript",
            ".ts" => "TypeScript",
            ".py" => "Python",
            ".java" => "Java",
            ".cpp" or ".cc" => "C++",
            ".h" => "C/C++",
            ".go" => "Go",
            ".rs" => "Rust",
            ".php" => "PHP",
            ".rb" => "Ruby",
            _ => "Unknown"
        };
    }
}
