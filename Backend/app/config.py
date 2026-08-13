from functools import lru_cache
from pathlib import Path

from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    model_config = SettingsConfigDict(
        env_prefix="EVOLIFE_",
        env_file=".env",
        env_file_encoding="utf-8",
        extra="ignore",
    )

    app_name: str = "EvoLife Analytics Backend"
    api_v1_prefix: str = "/api/v1"
    database_url: str = "sqlite:///./data/evolife_analytics.db"
    debug: bool = False

    @property
    def sqlite_path(self) -> Path | None:
        if self.database_url.startswith("sqlite:///"):
            relative = self.database_url.removeprefix("sqlite:///")
            if relative != ":memory:":
                return Path(relative)
        return None


@lru_cache
def get_settings() -> Settings:
    return Settings()
