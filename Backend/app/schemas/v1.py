from datetime import datetime, timezone
from typing import Optional

from pydantic import BaseModel, ConfigDict, Field


class StatsSnapshotIn(BaseModel):
    """Unity-compatible stats payload (camelCase field names)."""

    model_config = ConfigDict(populate_by_name=True)

    experimentId: str
    simulationTimeSeconds: float
    herbivoreCount: int
    predatorCount: int
    totalAlive: int
    timestampUtcUnix: float


class ExperimentCreate(BaseModel):
    name: str
    policy_herbivore: str = "scripted_baseline"
    policy_predator: str = "scripted_baseline"
    seed: int = 42
    notes: Optional[str] = None


class ExperimentRecord(BaseModel):
    id: str
    name: str
    policy_herbivore: str
    policy_predator: str
    seed: int
    notes: Optional[str] = None
    created_at: datetime = Field(default_factory=lambda: datetime.now(timezone.utc))
