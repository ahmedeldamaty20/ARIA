namespace ARIA.MCP.Models;
public record CodeChunk(
    string Id,
    string RepoUrl,
    string FilePath,
    string ChunkType,   // "function" | "class" | "file"
    string Name,
    string Content,
    int StartLine,
    int EndLine
);
