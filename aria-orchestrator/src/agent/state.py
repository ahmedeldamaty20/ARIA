from typing import Annotated
from langgraph.graph.message import add_messages
from langchain_core.messages import BaseMessage
from pydantic import BaseModel


class AgentState(BaseModel):
    """
    The state that is passed between each node in the graph.

    - messages: the entire conversation — the question + tool responses + agent response
    - repo_url: the repository we are currently working on
    - is_indexed: has the repository been indexed before?
    """

    messages: Annotated[list[BaseMessage], add_messages]
    repo_url: str = ""
    is_indexed: bool = False