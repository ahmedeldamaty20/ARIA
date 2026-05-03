using Services;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text;

namespace Tools;

[McpServerToolType]
public class FetchRepoTool(GitHubService github)
{
    [McpServerTool(Name = "github_fetch_repo")]
    [Description("Fetches the complete file tree of a GitHub repository. Use this tool when starting to explore a new repository.")]
    public async Task<string> FetchRepoAsync(
        [Description("The GitHub repository URL — example: https://github.com/owner/repo")] string repoUrl)
    {
        try
        {
            var (owner, repo) = GitHubService.ParseUrl(repoUrl);
            var files = await github.GetRepoFilesAsync(owner, repo);

            var sb = new StringBuilder();
            sb.AppendLine($"## {owner}/{repo} — File Structure");
            sb.AppendLine($"Total files: {files.Count}");
            sb.AppendLine();

            // Organize files by directory for a clearer structure
            var grouped = files
                .GroupBy(f => Path.GetDirectoryName(f) ?? "root")
                .OrderBy(g => g.Key);

            foreach (var group in grouped)
            {
                sb.AppendLine($"📁 {group.Key}/");
                foreach (var file in group.OrderBy(f => f))
                    sb.AppendLine($"   └── {Path.GetFileName(file)}");
            }

            Console.Error.WriteLine($"DEBUG: {sb.ToString()}");

            return sb.ToString();
        }
        catch (Exception ex)
        {
            // Return more detailed information for debugging. This is safe locally;
            // avoid exposing sensitive data in production logs.
            return $"Error fetching repository: {ex.Message}. Exception: {ex.ToString()}. Make sure the URL is correct and the repository is public.";
        }
    }
}