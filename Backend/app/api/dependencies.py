from collections.abc import Generator

from fastapi import Depends, Request
from sqlalchemy.orm import Session

from app.config import Settings, get_settings
from app.persistence.database import Database
from app.services.analytics_service import AnalyticsService


def get_app_settings() -> Settings:
    return get_settings()


def get_db(request: Request) -> Generator[Session, None, None]:
    database: Database = request.app.state.database
    session = database.session_factory()
    try:
        yield session
    finally:
        session.close()


def get_analytics_service(session: Session = Depends(get_db)) -> AnalyticsService:
    return AnalyticsService(session)
