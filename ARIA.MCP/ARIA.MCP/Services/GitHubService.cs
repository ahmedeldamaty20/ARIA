using System.Text.Json;

namespace ARIA.MCP.Services;
public class GitHubService(HttpClient httpClient)
{
    private static readonly HashSet<string> SupportedExtensions = [".cs", ".py", ".ts", ".js", ".java", ".go", ".rs", ".cpp", ".md"];

    // It gets the list of all files in the repo (recursively) and filters by supported extensions
    public async Task<List<string>> GetRepoFilesAsync(string owner, string repo)
    {
        var url = $"https://api.github.com/repos/{owner}/{repo}/git/trees/HEAD?recursive=1";
        var response = await httpClient.GetFromJsonAsync<JsonElement>(url);

        return response
            .GetProperty("tree")
            .EnumerateArray()
            .Where(item =>
                item.GetProperty("type").GetString() == "blob" &&
                SupportedExtensions.Contains(Path.GetExtension(item.GetProperty("path").GetString()!))
            )
            .Select(item => item.GetProperty("path").GetString()!)
            .ToList();
    }
}
