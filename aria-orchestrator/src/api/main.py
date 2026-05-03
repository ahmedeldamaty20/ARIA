from contextlib import asynccontextmanager
from fastapi import FastAPI, HTTPException
from langchain_core.messages import HumanMessage
from pydantic import BaseModel
from src.agent.graph import build_graph
from src.agent.state import AgentState


#  Lifespan — initialize the graph and tools before handling any requests 
@asynccontextmanager
async def lifespan(app: FastAPI):
    app.state.graph = await build_graph()
    yield
    # cleanup if needed (e.g., close MCP client subprocesses)

app = FastAPI(
    title="ARIA Agent API",
    lifespan=lifespan,
)


#  Request / Response models
class ChatRequest(BaseModel):
    repo_url: str
    question: str
    is_indexed: bool = False


class ChatResponse(BaseModel):
    answer: str
    is_indexed: bool   # Return to frontend to update indexing status after first question

#  Endpoints
@app.post("/chat", response_model=ChatResponse)
async def chat(request: ChatRequest):
    """
    Receives a question about a repository and returns an answer from the Agent.
    The Angular frontend sends:
    - repo_url: The repository URL
    - question: The user's question
    - is_indexed: Indicates whether the repository has been indexed before
        (to avoid re-indexing)
    Returns:
    - ChatResponse containing the agent's answer and an updated is_indexed status
        (set to True after the first question, as the repository becomes indexed)
    Raises:
    - HTTPException: Status code 500 if any error occurs during processing
    """
   
    try:
        initial_state = AgentState(
            messages=[HumanMessage(content=request.question)],
            repo_url=request.repo_url,
            is_indexed=request.is_indexed,
        )

        final_state = await app.state.graph.ainvoke(initial_state)

        # The final response from the agent is expected to be in the last message of the conversation history
        answer = final_state["messages"][-1].content

        return ChatResponse(
            answer=answer,
            is_indexed=True,  # After the first question, the repo will be indexed
        )

    except Exception as ex:
        raise HTTPException(status_code=500, detail=str(ex))


@app.get("/health")
async def health():
    return {"status": "ok"}