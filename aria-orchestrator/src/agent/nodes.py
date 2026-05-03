from langchain_openai import ChatOpenAI
from langchain_core.messages import SystemMessage
from langgraph.prebuilt import ToolNode

from src.agent.state import AgentState
from src.config import settings

SYSTEM_PROMPT = """You are an expert code analyst. You help developers understand any GitHub repository.
 
## Tool Selection Rules — follow exactly:
 
1. User asks about files/structure/overview → use github_fetch_repo
2. User explicitly asks to "index" OR you need search but repo not indexed → use github_index_repo
3. User asks about specific logic, implementation, "where is X", "how does X work" → use github_search_code
4. User asks about a specific file by name → use github_fetch_file
5. User asks about libraries, packages, dependencies → use github_analyze_deps
 
## CRITICAL Rules:
- NEVER call github_index_repo for simple structure questions
- Always cite file path and line numbers in your answers
- If repo is not indexed (is_indexed=False) and user asks a search question → index first, then search
- Be specific — show actual code in your response when relevant
"""


def make_agent_node(tools: list):
    """
    Creates the agent node — connects the LLM with the tools.

    bind_tools tells the LLM:
    "If you need information, use one of these tools"
    """

    llm = ChatOpenAI(
        model=settings.model_name,
        api_key=settings.OPENAI_API_KEY,
        temperature=0 
    ).bind_tools(tools)

    async def agent_node(state: AgentState) -> dict:
        # Build messages from scratch each time 
        # based on the conversation history in the state, and add the system prompt with repo context
        context = ""
        if state.repo_url:
            context = (
                f"\nCurrent repo: {state.repo_url}"
                f"\nRepo indexed in Pinecone: {state.is_indexed}"
            )
 
        system = SystemMessage(content=SYSTEM_PROMPT + context)
        messages = [system] + list(state.messages)
 
        response = await llm.ainvoke(messages)
        return {"messages": [response]}
 
    return agent_node


def make_tools_node(tools: list) -> ToolNode:
    """
    Creates the tools node — a pre-built node from LangGraph that handles tool execution.

    It takes the tool calls from the LLM and executes the actual tools.
    """
    return ToolNode(tools)


def should_continue(state: AgentState) -> str:
    """
    Router — decides the next step:
    - If the LLM requests a tool → go to the tools node
    - If the LLM responds directly → end the graph and return the answer
    """
    last_message = state.messages[-1]

    if hasattr(last_message, "tool_calls") and last_message.tool_calls:
        return "tools"

    return "end"