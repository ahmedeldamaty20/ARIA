using Models;
using Services;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace Tools;

[McpServerToolType]
public class IndexRepoTool(GitHubService github, ChunkingService chunker, EmbeddingService embedder, PineconeService pinecone)
{
    [McpServerTool(Name = "github_index_repo")]
    [Description(
        "Performs full indexing of the repository in Pinecone — splits the code into chunks and generates embeddings. " +
        "You must run it once before using github_search_code. It may take a few minutes depending on the size of the repository.")]
    public async Task<string> IndexRepoAsync(
        [Description("GitHub repository URL")]
        string repoUrl,
        [Description("Should the old indexing be deleted and a new one created? (default: false)")]
        bool forceReindex = false)
    {
        try
        {
            var (owner, repo) = GitHubService.ParseUrl(repoUrl);

            if (forceReindex)
                await pinecone.DeleteRepoChunksAsync(repoUrl);

            Console.Error.WriteLine($"DEBUG: begin");

            // 1. Get the list of files
            var files = await github.GetRepoFilesAsync(owner, repo);

            var allChunks = new List<CodeChunk>();
            var processedFiles = 0;

            // 2. Read each file and split it into chunks
            foreach (var filePath in files)
            {
                try
                {
                    var content = await github.GetFileContentAsync(owner, repo, filePath);
                    var chunks = chunker.ChunkFile(repoUrl, filePath, content);
                    allChunks.AddRange(chunks);
                    processedFiles++;

                    // Rate limit safety
                    await Task.Delay(100);
                }
                catch
                {
                    // File couldn't be read — skip
                }
            }

            Console.Error.WriteLine($"DEBUG: Processed {processedFiles} files");

            // 3. Generate embeddings (batch)
            var texts = allChunks.Select(c => $"{c.FilePath}\n{c.Name}\n{c.Content}").ToList();
            var embeddings = await embedder.EmbedBatchAsync(texts);

            // 4. Upload to Pinecone
            await pinecone.UpsertChunksAsync(allChunks, embeddings);

            Console.Error.WriteLine($"DEBUG: Uploaded {allChunks.Count} chunks to Pinecone");


            return $"""
                Indexing complete!
                - Files processed: {processedFiles}/{files.Count}
                - Total chunks: {allChunks.Count}
                - Repo: {owner}/{repo}
                
                Ready! You can now use github_search_code to search the code.
                """;
        }
        catch (Exception ex)
        {
            return $"Indexing failed: {ex.Message}";
        }
    }
}