from contextlib import asynccontextmanager

from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware

from app.api.routes import analytics, creatures, generations, runs, snapshots, v1
from app.config import Settings, get_settings
from app.persistence.database import Database


def create_app(settings: Settings | None = None) -> FastAPI:
    settings = settings or get_settings()

    @asynccontextmanager
    async def lifespan(app: FastAPI):
        database = Database(settings.database_url)
        database.create_tables()
        app.state.database = database
        yield

    app = FastAPI(
        title=settings.app_name,
        description="Experiment records and simulation statistics for EvoLife.",
        version="0.2.0",
        lifespan=lifespan,
    )

    app.add_middleware(
        CORSMiddleware,
        allow_origins=["*"],
        allow_credentials=True,
        allow_methods=["*"],
        allow_headers=["*"],
    )

    app.include_router(v1.router, prefix=settings.api_v1_prefix)
    app.include_router(runs.router, prefix=settings.api_v1_prefix)
    app.include_router(snapshots.router, prefix=settings.api_v1_prefix)
    app.include_router(creatures.router, prefix=settings.api_v1_prefix)
    app.include_router(generations.router, prefix=settings.api_v1_prefix)
    app.include_router(analytics.router, prefix=settings.api_v1_prefix)

    @app.get("/health")
    def health() -> dict[str, str]:
        return {"status": "ok"}

    return app


app = create_app()
