using System.Text.Json;

namespace ARIA.MCP.Services;

public class GitHubService(HttpClient httpClient)
{
    private static readonly HashSet<string> SupportedExtensions =
        [".cs", ".py", ".ts", ".js", ".java", ".go", ".rs", ".cpp", ".md"];

    private const long MaxFileSizeBytes = 100_000; // 100KB per file

    // Get a list of all files in the repository with supported extensions and under the size limit
    public async Task<List<string>> GetRepoFilesAsync(string owner, string repo)
    {
        var url = $"https://api.github.com/repos/{owner}/{repo}/git/trees/HEAD?recursive=1";
        var response = await httpClient.GetFromJsonAsync<JsonElement>(url);

        return response
            .GetProperty("tree")
            .EnumerateArray()
            .Where(item =>
                item.GetProperty("type").GetString() == "blob" &&
                item.GetProperty("size").GetInt64() < MaxFileSizeBytes &&
                SupportedExtensions.Contains(
                    Path.GetExtension(item.GetProperty("path").GetString()!)))
            .Select(item => item.GetProperty("path").GetString()!)
            .ToList();
    }

    // Get the content of multiple files concurrently, with rate limit safety
    public async Task<Dictionary<string, string>> GetFilesContentAsync(
        string owner, string repo, List<string> paths)
    {
        var results = new Dictionary<string, string>();
        var semaphore = new SemaphoreSlim(5); // max 5 concurrent requests
        var lockObj = new object();

        await Task.WhenAll(paths.Select(async path =>
        {
            await semaphore.WaitAsync();
            try
            {
                var content = await GetFileContentAsync(owner, repo, path);
                lock (lockObj)
                    results[path] = content;
            }
            catch
            {
                // File couldn't be read — skip
            }
            finally
            {
                semaphore.Release();
                await Task.Delay(50); // rate limit safety: 50ms delay between requests
            }
        }));

        return results;
    }

    // Get the content of a single file
    public async Task<string> GetFileContentAsync(string owner, string repo, string path)
    {
        var url = $"https://api.github.com/repos/{owner}/{repo}/contents/{path}";
        var response = await httpClient.GetFromJsonAsync<JsonElement>(url);

        var base64 = response.GetProperty("content").GetString()!.Replace("\n", "");
        return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(base64));
    }

    // Get common dependency files (like package.json, requirements.txt, and .csproj files) and their content
    public async Task<Dictionary<string, string>> GetDependenciesAsync(
        string owner, string repo)
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

        // get all .csproj files (can be multiple in a repo) and read their content
        var allFiles = await GetRepoFilesAsync(owner, repo);
        var csprojFiles = allFiles.Where(f => f.EndsWith(".csproj")).ToList();
        var csprojDeps = await GetFilesContentAsync(owner, repo, csprojFiles);

        foreach (var (path, content) in csprojDeps)
            deps[path] = content;

        return deps;
    }

    //  helper: convert "github.com/user/repo" → (owner, repo)
    public static (string owner, string repo) ParseUrl(string url)
    {
        var uri = new Uri(url.StartsWith("http") ? url : "https://" + url);
        var parts = uri.AbsolutePath.Trim('/').Split('/');
        return (parts[0], parts[1]);
    }
}