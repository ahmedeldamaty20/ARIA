using System.Text.Json;

namespace Services;

// It's responsible for calling OpenAI API to get embeddings for code chunks and dependency files. It supports both single embedding and batch embedding (which is faster and more cost-effective).
public class EmbeddingService(HttpClient httpClient)
{
    private const string Model = "text-embedding-3-small";    // 1536 dimensions
    private const int BatchSize = 20;                         // max per request

    // For a single text, we can just call the batch method with one item
    public async Task<float[]> EmbedAsync(string text)
    {
        var results = await EmbedBatchAsync([text]);
        return results[0];
    }

    // For multiple texts, we split them into batches of 20 (the max allowed by OpenAI) and call the API for each batch. We also add a small delay between batches to avoid hitting rate limits.
    public async Task<List<float[]>> EmbedBatchAsync(List<string> texts)
    {
        var allEmbeddings = new List<float[]>();

        // we need to preserve the order of the input texts, so we include the index in the request and sort the results by index before extracting the embeddings
        for (int i = 0; i < texts.Count; i += BatchSize)
        {
            var batch = texts.Skip(i).Take(BatchSize).ToList();

            var request = new
            {
                model = Model,
                input = batch
            };

            var response = await httpClient.PostAsJsonAsync("https://api.openai.com/v1/embeddings", request);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<JsonElement>();

            var batchEmbeddings = result
                .GetProperty("data")
                .EnumerateArray()
                .OrderBy(item => item.GetProperty("index").GetInt32())
                .Select(item => item
                    .GetProperty("embedding")
                    .EnumerateArray()
                    .Select(v => v.GetSingle())
                    .ToArray())
                .ToList();

            allEmbeddings.AddRange(batchEmbeddings);

            // add a small delay between batches to avoid hitting rate limits (OpenAI allows 60 requests per minute for embeddings, so we can do 1 request every second safely, but we add a bit more buffer just in case)
            if (i + BatchSize < texts.Count)
                await Task.Delay(200);
        }

        return allEmbeddings;
    }
}
