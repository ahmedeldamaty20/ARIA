using Models;
using Pinecone;

namespace Services;

// It's responsible for interacting with Pinecone vector database to store and search code chunks based on their embeddings. It has methods for upserting chunks, searching for similar chunks given a query embedding, and deleting all chunks related to a specific repo (when the repo is updated). 
public class PineconeService(IConfiguration config)
{
    private readonly PineconeClient _client = new(config["Pinecone:ApiKey"]!);

    private const string IndexName = "legal-contract-ai";

    // Upload or update chunks in Pinecone index (upsert)
    public async Task UpsertChunksAsync(List<CodeChunk> chunks, List<float[]> embeddings)
    {
        try
        {
            var index = await _client.GetIndex(IndexName);

            var vectors = chunks.Zip(embeddings, (chunk, embedding) => new Vector
            {
                Id = chunk.Id,
                Values = embedding,
                Metadata = new MetadataMap
                {
                    ["repo_url"] = chunk.RepoUrl,
                    ["file_path"] = chunk.FilePath,
                    ["chunk_type"] = chunk.ChunkType,
                    ["name"] = chunk.Name,
                    ["content"] = chunk.Content[..Math.Min(1000, chunk.Content.Length)],
                    ["start_line"] = chunk.StartLine,
                    ["end_line"] = chunk.EndLine
                }
            }).ToArray();

            // Pinecone accepts up to 100 vectors per request, so we batch them
            const int batchSize = 100;
            for (int i = 0; i < vectors.Length; i += batchSize)
            {
                var batch = vectors.Skip(i).Take(batchSize).ToArray();
                await index.Upsert(batch);
            }
        }
        catch (Exception ex)
        {

        }
    }

    // Search for similar chunks given a query embedding and a repo URL to restrict the search to the same repo. Returns a list of SearchResult with the most relevant chunks based on cosine similarity score.
    public async Task<List<SearchResultResponse>> SearchAsync(float[] queryEmbedding, string repoUrl, int topK = 5)
    {
        var index = await _client.GetIndex(IndexName);

        var filter = new MetadataMap
        {
            // We want to restrict the search to chunks that belong to the same repo
            ["repo_url"] = new MetadataMap
            {
                ["$eq"] = repoUrl
            }
        };

        var response = await index.Query(
            new ReadOnlyMemory<float>(queryEmbedding),
            (uint)topK,
            filter,
            includeMetadata: true
        );

        return response
        .Where(m => m.Metadata is not null)
        .Select(m => new SearchResultResponse(
            FilePath: m.Metadata!["file_path"].Inner?.ToString() ?? "",
            ChunkType: m.Metadata!["chunk_type"].Inner?.ToString() ?? "",
            Name: m.Metadata!["name"].Inner?.ToString() ?? "",
            Content: m.Metadata!["content"].Inner?.ToString() ?? "",
            Score: (float)m.Score
        ))
        .ToList();
    }

    // Delete all chunks related to a specific repo (used when the repo is updated to remove old chunks that might be outdated)
    public async Task DeleteRepoChunksAsync(string repoUrl)
    {
        var index = await _client.GetIndex(IndexName);

        var filter = new MetadataMap
        {
            ["repo_url"] = new MetadataMap { ["$eq"] = repoUrl }
        };

        await index.Delete(filter);
    }
}
