from pydantic_settings import BaseSettings, SettingsConfigDict

class Settings(BaseSettings):
    model_config = SettingsConfigDict(env_file=".env", env_file_encoding="utf-8")

    OPENAI_API_KEY: str
    mcp_server_command: str = "E:\\Projects\\ARIA\\ARIA.MCP\\ARIA.MCP\\bin\\Release\\net9.0\\ARIA.MCP.exe"
    mcp_server_args: str = ""

    model_name: str = "gpt-4o-mini"

settings = Settings()