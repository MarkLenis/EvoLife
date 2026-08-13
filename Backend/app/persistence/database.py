from collections.abc import Generator
from pathlib import Path

from sqlalchemy import create_engine, event
from sqlalchemy.orm import Session, sessionmaker

from app.config import Settings, get_settings
from app.persistence.models import Base


def _configure_sqlite_engine(engine) -> None:
    if engine.dialect.name != "sqlite":
        return

    @event.listens_for(engine, "connect")
    def set_sqlite_pragma(dbapi_connection, _connection_record) -> None:
        cursor = dbapi_connection.cursor()
        cursor.execute("PRAGMA foreign_keys=ON")
        cursor.close()


def build_engine(database_url: str):
    connect_args = {"check_same_thread": False} if database_url.startswith("sqlite") else {}
    return create_engine(database_url, connect_args=connect_args)


class Database:
    def __init__(self, database_url: str) -> None:
        self.engine = build_engine(database_url)
        _configure_sqlite_engine(self.engine)
        self.session_factory = sessionmaker(
            bind=self.engine,
            autoflush=False,
            autocommit=False,
            expire_on_commit=False,
        )

    def create_tables(self) -> None:
        if self.engine.dialect.name == "sqlite" and self.engine.url.database not in (None, ":memory:"):
            Path(self.engine.url.database).parent.mkdir(parents=True, exist_ok=True)
        Base.metadata.create_all(bind=self.engine)
        _ensure_sqlite_columns(self.engine)


def _ensure_sqlite_columns(engine) -> None:
    """Add columns introduced after the first SQLite file was created."""
    if engine.dialect.name != "sqlite":
        return

    statements = (
        (
            "creature_life_records",
            "policy_kind",
            "ALTER TABLE creature_life_records ADD COLUMN policy_kind VARCHAR(64)",
        ),
    )
    with engine.begin() as connection:
        for table, column, ddl in statements:
            rows = connection.exec_driver_sql(f"PRAGMA table_info({table})").fetchall()
            existing = {row[1] for row in rows}
            if column not in existing and rows:
                connection.exec_driver_sql(ddl)

    def get_session(self) -> Generator[Session, None, None]:
        session = self.session_factory()
        try:
            yield session
        finally:
            session.close()
