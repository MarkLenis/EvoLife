"""EvoLife analytics FastAPI application."""

from fastapi import FastAPI

from app.api.routes import router as api_router

app = FastAPI(
    title="EvoLife Analytics",
    description="Experiment records and simulation statistics for EvoLife.",
    version="0.1.0",
)

app.include_router(api_router, prefix="/api/v1")


@app.get("/health")
def health() -> dict[str, str]:
    return {"status": "ok"}
