namespace ARIA.MCP.Models;
public record SearchResultResponse(
    string FilePath,
    string ChunkType,
    string Name,
    string Content,
    float Score
);