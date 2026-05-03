using Models;
using System.Text.RegularExpressions;

namespace Services;

// It's responsible for splitting code files into smaller chunks (functions, classes, or whole file) to be indexed and searched later.
public class ChunkingService
{
    // It uses simple regex patterns to find function/method definitions in C#, Python, and TypeScript/JavaScript files.
    private static readonly Regex CSharpMethod = new(
        @"((?:public|private|protected|internal|static|async|virtual|override)\s+)+\w+\s+\w+\s*\([^)]*\)\s*\{",
        RegexOptions.Multiline);

    private static readonly Regex PythonFunction = new(
        @"^(async\s+)?def\s+\w+\s*\([^)]*\).*:$",
        RegexOptions.Multiline);

    private static readonly Regex PythonClass = new(
        @"^class\s+\w+.*:$",
        RegexOptions.Multiline);

    private static readonly Regex TsFunction = new(
        @"(export\s+)?(async\s+)?function\s+\w+|const\s+\w+\s*=\s*(async\s+)?\(",
        RegexOptions.Multiline);


    public List<CodeChunk> ChunkFile(string repoUrl, string filePath, string content)
    {
        var ext = Path.GetExtension(filePath).ToLower();

        return ext switch
        {
            ".cs" => ChunkCSharp(repoUrl, filePath, content),
            ".py" => ChunkPython(repoUrl, filePath, content),
            ".ts" or ".js" => ChunkTypeScript(repoUrl, filePath, content),
            _ => [FallbackChunk(repoUrl, filePath, content)]
        };
    }

    //  C# chunker
    private List<CodeChunk> ChunkCSharp(string repo, string path, string content)
    {
        var chunks = new List<CodeChunk>();
        var lines = content.Split('\n');
        var matches = CSharpMethod.Matches(content);

        for (int i = 0; i < matches.Count; i++)
        {
            var match = matches[i];
            var startPos = match.Index;
            var endPos = i + 1 < matches.Count ? matches[i + 1].Index : content.Length;

            var chunkContent = content[startPos..endPos].Trim();
            if (chunkContent.Length > 3000)               // cap the chunk size to 3000 chars to avoid very long methods (we can improve this later by trying to split the method into smaller parts)
                chunkContent = chunkContent[..3000] + "\n// ... truncated";

            var startLine = content[..startPos].Count(c => c == '\n') + 1;
            var name = ExtractMethodName(match.Value);

            chunks.Add(new CodeChunk(
                Id: Guid.NewGuid().ToString(),
                RepoUrl: repo,
                FilePath: path,
                ChunkType: "function",
                Name: name,
                Content: chunkContent,
                StartLine: startLine,
                EndLine: startLine + chunkContent.Count(c => c == '\n')
            ));
        }

        // If no methods were found, we create a single chunk for the whole file (up to 4000 chars) to ensure it's indexed and searchable.
        if (chunks.Count == 0)
            chunks.Add(FallbackChunk(repo, path, content));

        return chunks;
    }

    //  Python chunker
    private List<CodeChunk> ChunkPython(string repo, string path, string content)
    {
        var chunks = new List<CodeChunk>();
        var lines = content.Split('\n');
        var starts = new List<(int lineIndex, string name, string type)>();

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].TrimStart();

            if (PythonClass.IsMatch(line))
            {
                var name = line.Split(' ')[1].Split('(')[0].Split(':')[0];
                starts.Add((i, name, "class"));
            }
            else if (PythonFunction.IsMatch(line))
            {
                var name = line.TrimStart().Replace("async ", "")
                               .Replace("def ", "").Split('(')[0];
                starts.Add((i, name, "function"));
            }
        }

        for (int i = 0; i < starts.Count; i++)
        {
            var (startIdx, name, type) = starts[i];
            var endIdx = i + 1 < starts.Count ? starts[i + 1].lineIndex : lines.Length;

            var chunkLines = lines[startIdx..endIdx];
            var chunkContent = string.Join('\n', chunkLines).Trim();

            chunks.Add(new CodeChunk(
                Id: Guid.NewGuid().ToString(),
                RepoUrl: repo,
                FilePath: path,
                ChunkType: type,
                Name: name,
                Content: chunkContent,
                StartLine: startIdx + 1,
                EndLine: endIdx
            ));
        }

        if (chunks.Count == 0)
            chunks.Add(FallbackChunk(repo, path, content));

        return chunks;
    }

    //  TypeScript / JavaScript chunker
    private List<CodeChunk> ChunkTypeScript(string repo, string path, string content)
    {
        // For simplicity, we only look for function declarations and const arrow functions. We can improve this later by adding support for classes, interfaces, and other constructs.
        var chunks = new List<CodeChunk>();
        var matches = TsFunction.Matches(content);

        for (int i = 0; i < matches.Count; i++)
        {
            var startPos = matches[i].Index;
            var endPos = i + 1 < matches.Count
                ? matches[i + 1].Index
                : content.Length;

            var chunkContent = content[startPos..endPos].Trim();
            var startLine = content[..startPos].Count(c => c == '\n') + 1;

            chunks.Add(new CodeChunk(
                Id: Guid.NewGuid().ToString(),
                RepoUrl: repo,
                FilePath: path,
                ChunkType: "function",
                Name: matches[i].Value[..Math.Min(50, matches[i].Value.Length)].Trim(),
                Content: chunkContent,
                StartLine: startLine,
                EndLine: startLine + chunkContent.Count(c => c == '\n')
            ));
        }

        if (chunks.Count == 0)
            chunks.Add(FallbackChunk(repo, path, content));

        return chunks;
    }

    // Fallback chunker for unsupported file types or files without identifiable functions/classes. It creates a single chunk for the whole file (up to 4000 chars) to ensure it's indexed and searchable.
    private CodeChunk FallbackChunk(string repo, string path, string content) => new(
        Id: Guid.NewGuid().ToString(),
        RepoUrl: repo,
        FilePath: path,
        ChunkType: "file",
        Name: Path.GetFileName(path),
        Content: content.Length > 4000 ? content[..4000] + "\n// truncated" : content,
        StartLine: 1,
        EndLine: content.Count(c => c == '\n') + 1
    );

    // ------------------------------------------------------------------ //
    private static string ExtractMethodName(string signature)
    {
        var match = Regex.Match(signature, @"\s(\w+)\s*\(");
        return match.Success ? match.Groups[1].Value : "unknown";
    }
}