from langgraph.graph import StateGraph, END

from src.agent.state import AgentState
from src.agent.nodes import make_agent_node, make_tools_node, should_continue
from src.agent.tools import load_mcp_tools

async def build_graph():
    """
    Builds the LangGraph workflow:

    [agent] → should_continue → [tools] → [agent] → ... → END

    This is the ReAct loop:
    - Agent thinks and decides whether to use a tool or not
    - If a tool is used → return to the agent with the result
    - If it responds directly → END

    CRITICAL:
    - Reuse previous tool outputs when enough context exists
    - Avoid redundant tool calls
    
    Be concise unless user asks for detail
    """

    # 1. Load the tools from the MCP server
    tools = await load_mcp_tools()

    # 2. Build the nodes
    agent_node = make_agent_node(tools)
    tools_node = make_tools_node(tools)

    # 3. Build the graph
    graph = StateGraph(AgentState)

    graph.add_node("agent", agent_node)
    graph.add_node("tools", tools_node)

    # 4. Add the edges
    graph.set_entry_point("agent")  # Start always from the agent

    graph.add_conditional_edges(
        "agent",
        should_continue,
        {
            "tools": "tools",  # The LLM requests a tool → go here
            "end": END,        # The LLM responds directly → end
        },
    )

    # After executing the tools, we always go back to the agent to decide the next step
    graph.add_edge("tools", "agent")

    return graph.compile()