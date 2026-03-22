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

    // It tries to find common dependency files and returns their content in a dictionary (package.json → content, requirements.txt → content, *.csproj → content)
    public async Task<Dictionary<string, string>> GetDependenciesAsync(string owner, string repo)
    {
        var deps = new Dictionary<string, string>();
        var candidates = new[] { "package.json", "requirements.txt" };

        foreach (var file in candidates)
        {
            try
            {
                var content = await GetFileContentAsync(owner, repo, file);
                deps[file] = content;
            }
            catch { /* The file might not exist, so we ignore errors */ }
        }

        // For .csproj files, we need to find them first and then get their content
        var allFiles = await GetRepoFilesAsync(owner, repo);
        foreach (var csproj in allFiles.Where(f => f.EndsWith(".csproj")))
        {
            var content = await GetFileContentAsync(owner, repo, csproj);
            deps[csproj] = content;
        }

        return deps;
    }
}
