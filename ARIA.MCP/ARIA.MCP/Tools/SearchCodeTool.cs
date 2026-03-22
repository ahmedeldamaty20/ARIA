using ARIA.MCP.Services;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text;

namespace ARIA.MCP.Tools;

[McpServerToolType]
public class SearchCodeTool(EmbeddingService embedder, PineconeService pinecone)
{
    [McpServerTool(Name = "github_search_code")]
    [Description(
        "Searches for specific code in a GitHub repo using semantic search. " +
        "Make sure to run github_index_repo first. " +
        "Examples: 'authentication logic', 'database connection', 'error handling'.")]
    public async Task<string> SearchCodeAsync(
        [Description("GitHub repository URL")] string repoUrl,
        [Description("The query or code snippet you want to search for — either natural language or function name")] string query,
        [Description("Number of results (default: 5, max: 10)")] int topK = 5)
    {
        try
        {
            topK = Math.Min(topK, 10);

            // Generate embedding for the query
            var queryEmbedding = await embedder.EmbedAsync(query);

            // Perform semantic search in Pinecone
            var results = await pinecone.SearchAsync(queryEmbedding, repoUrl, topK);

            if (results.Count == 0)
                return "No results found. Make sure you have run github_index_repo on this repo.";

            var sb = new StringBuilder();
            sb.AppendLine($"## Search Results for: \"{query}\"");
            sb.AppendLine($"Found {results.Count} relevant chunks:\n");

            foreach (var (result, i) in results.Select((r, i) => (r, i + 1)))
            {
                sb.AppendLine($"### {i}. {result.Name} ({result.ChunkType})");
                sb.AppendLine($"**File:** `{result.FilePath}`");
                sb.AppendLine($"**Relevance:** {result.Score:P0}");
                sb.AppendLine();
                sb.AppendLine("```");
                sb.AppendLine(result.Content);
                sb.AppendLine("```");
                sb.AppendLine();
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            return $"Search failed: {ex.Message}";
        }
    }
}