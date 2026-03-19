using ARIA.MCP.Services;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace ARIA.MCP.Tools;

[McpServerToolType]
public class FetchFileTool(GitHubService github)
{
    [McpServerTool(Name = "github_fetch_file")]
    [Description("Fetches the content of a specific file from the repository. Use it when you know the name of the file you want to read.")]
    public async Task<string> FetchFileAsync(
    [Description("GitHub repository URL")] string repoUrl,
    [Description("Full file path — example: src/Services/AuthService.cs")] string filePath)
    {
        try
        {
            var (owner, repo) = GitHubService.ParseUrl(repoUrl);
            var content = await github.GetFileContentAsync(owner, repo, filePath);

            return $"## {filePath}\n\n```\n{content}\n```";
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return $"File not found: {filePath}. Use github_fetch_repo first to see the available files.";
        }
        catch (Exception ex)
        {
            return $"Error fetching file: {ex.Message}";
        }
    }
}