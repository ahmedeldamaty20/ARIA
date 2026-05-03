import os

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
                "args": settings.mcp_server_args,
                "transport": "stdio",
            }
        }
    )

    # Debug: ensure important env vars are present for the MCP subprocess
    print(f"Starting MCP server with command: {settings.mcp_server_command} {' '.join(settings.mcp_server_args)}")
    has_token = "GITHUB_TOKEN" in os.environ
    print("GITHUB_TOKEN present in Python process:", has_token)
    if has_token:
        t = os.environ.get("GITHUB_TOKEN") or ""
        print("GITHUB_TOKEN prefix:", (t[:4] + "...") if len(t) >= 4 else t)

    tools = await client.get_tools()

    print(f"\n✅ Loaded {len(tools)} tools:")
    for tool in tools:
        print(f"  - {tool.name}: {tool.description[:60]}...")        

    # verify that all expected tools are present
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