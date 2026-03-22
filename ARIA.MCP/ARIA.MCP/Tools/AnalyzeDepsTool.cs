using ARIA.MCP.Services;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ARIA.MCP.Tools;

[McpServerToolType]
public class AnalyzeDepsTool(GitHubService github)
{
    [McpServerTool(Name = "github_analyze_deps")]
    [Description("Analyzes the repository's dependencies and explains which libraries are used and their roles.")]
    public async Task<string> AnalyzeDepsAsync([Description("GitHub repository URL")] string repoUrl)
    {
        try
        {
            var (owner, repo) = GitHubService.ParseUrl(repoUrl);
            var depFiles = await github.GetDependenciesAsync(owner, repo);

            if (depFiles.Count == 0)
                return "No dependency files found (package.json, .csproj, requirements.txt).";

            var sb = new StringBuilder();
            sb.AppendLine($"## Dependencies Analysis — {owner}/{repo}\n");

            foreach (var (fileName, content) in depFiles)
            {
                sb.AppendLine($"### {fileName}");

                if (fileName.EndsWith("package.json"))
                    sb.AppendLine(ParsePackageJson(content));
                else if (fileName.EndsWith(".csproj"))
                    sb.AppendLine(ParseCsproj(content));
                else if (fileName.EndsWith("requirements.txt"))
                    sb.AppendLine(ParseRequirementsTxt(content));

                sb.AppendLine();
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            return $"Analysis failed: {ex.Message}";
        }
    }

    // ------------------------------------------------------------------ //
    private static string ParsePackageJson(string content)
    {
        var doc = JsonDocument.Parse(content);
        var sb = new StringBuilder();
        var root = doc.RootElement;

        if (root.TryGetProperty("dependencies", out var deps))
        {
            sb.AppendLine("**Dependencies:**");
            foreach (var dep in deps.EnumerateObject())
                sb.AppendLine($"- `{dep.Name}`: {dep.Value}");
        }

        if (root.TryGetProperty("devDependencies", out var devDeps))
        {
            sb.AppendLine("\n**Dev Dependencies:**");
            foreach (var dep in devDeps.EnumerateObject())
                sb.AppendLine($"- `{dep.Name}`: {dep.Value}");
        }

        return sb.ToString();
    }

    private static string ParseCsproj(string content)
    {
        var doc = XDocument.Parse(content);
        var packages = doc.Descendants("PackageReference");
        var sb = new StringBuilder();

        sb.AppendLine("**NuGet Packages:**");
        foreach (var pkg in packages)
        {
            var name = pkg.Attribute("Include")?.Value ?? "unknown";
            var version = pkg.Attribute("Version")?.Value ?? "?";
            sb.AppendLine($"- `{name}` v{version}");
        }

        return sb.ToString();
    }

    private static string ParseRequirementsTxt(string content)
    {
        var sb = new StringBuilder();
        sb.AppendLine("**Python Packages:**");

        foreach (var line in content.Split('\n'))
        {
            var trimmed = line.Trim();
            if (!string.IsNullOrEmpty(trimmed) && !trimmed.StartsWith('#'))
                sb.AppendLine($"- `{trimmed}`");
        }

        return sb.ToString();
    }
}