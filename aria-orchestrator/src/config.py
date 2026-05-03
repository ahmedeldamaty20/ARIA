from pydantic_settings import BaseSettings, SettingsConfigDict
from dotenv import load_dotenv

# Load .env into the process environment so subprocesses inherit variables
load_dotenv()

class Settings(BaseSettings):
    # Allow extra environment variables (e.g., GITHUB_TOKEN) so .env can contain
    # provider tokens that are consumed by subprocesses or other libraries.
    model_config = SettingsConfigDict(env_file=".env", env_file_encoding="utf-8", extra="allow")

    OPENAI_API_KEY: str
    # mcp_server_command: str = "dotnet"
    # mcp_server_args: list[str] = ["run", "--project", r"D:\Projects\ARIA\aria-toolbox\aria-toolbox\aria-toolbox.csproj", "--no-build"]

    mcp_server_command: str = "dotnet"

    mcp_server_args: list[str] = [
        r"D:\Projects\ARIA\aria-toolbox\out\aria-toolbox.dll"
    ]
    
    model_name: str = "gpt-4o-mini"

settings = Settings()