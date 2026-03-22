import asyncio
from langchain_mcp_adapters.client import MultiServerMCPClient
from langchain_core.tools import BaseTool
from src.config import settings

async def load_mcp_tools() -> list[BaseTool]:
    """
    Starts the .NET MCP Server as a subprocess and retrieves the tools from it.

    The transport here is stdio:
    - Python runs dotnet run
    - Communicates with it via stdin/stdout
    - The MCP protocol adapts automatically
    """

    client = MultiServerMCPClient(
        {
            "codebase": {
                "command": settings.mcp_server_command,
                "args": settings.mcp_server_args.split(),
                "transport": "stdio",
            }
        }
    )

    print(f"Starting MCP server with command: {settings.mcp_server_command} {' '.join(settings.mcp_server_args.split())}")

    tools = await client.get_tools()

    for tool in tools:
        print(f"Loaded MCP tool: {tool.name} - {tool.description}")

    # verify إن الـ tools اتحملت صح
    tool_names = [t.name for t in tools]
    expected = [
        "github_fetch_repo",
        "github_fetch_file",
        "github_index_repo",
        "github_search_code",
        "github_analyze_deps",
    ]

    missing = [t for t in expected if t not in tool_names]
    if missing:
        raise RuntimeError(f"MCP tools missing: {missing}")

    return tools

if __name__ == "__main__":
    asyncio.run(load_mcp_tools())