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

    // Helper method to parse GitHub repo URL and extract owner and repo name
    public static (string owner, string repo) ParseUrl(string url)
    {
        var uri = new Uri(url.StartsWith("http") ? url : "https://" + url);
        var parts = uri.AbsolutePath.Trim('/').Split('/');
        return (parts[0], parts[1]);
    }

    // Gets the content of a file in the repo using GitHub API (base64 encoded)
    public async Task<string> GetFileContentAsync(string owner, string repo, string path)
    {
        var url = $"https://api.github.com/repos/{owner}/{repo}/contents/{path}";
        var response = await httpClient.GetFromJsonAsync<JsonElement>(url);

        var base64 = response.GetProperty("content").GetString()!.Replace("\n", "");

        return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(base64));
    }
}
