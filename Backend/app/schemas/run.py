from datetime import datetime
from enum import Enum
from typing import Any

from pydantic import BaseModel, ConfigDict, Field


class ExtensibleModel(BaseModel):
    model_config = ConfigDict(extra="allow")


class RunStatus(str, Enum):
    PENDING = "pending"
    RUNNING = "running"
    COMPLETED = "completed"
    FAILED = "failed"
    CANCELLED = "cancelled"


class SimulationRunCreate(ExtensibleModel):
    experiment_name: str = Field(..., min_length=1, max_length=255)
    random_seed: int | None = None
    configuration: dict[str, Any] = Field(default_factory=dict)
    status: RunStatus = RunStatus.RUNNING
    metadata: dict[str, Any] = Field(default_factory=dict)


class SimulationRunFinish(ExtensibleModel):
    status: RunStatus = RunStatus.COMPLETED
    finished_at: datetime | None = None


class SimulationRunResponse(ExtensibleModel):
    run_id: str
    started_at: datetime
    finished_at: datetime | None = None
    experiment_name: str
    random_seed: int | None = None
    configuration: dict[str, Any] = Field(default_factory=dict)
    status: RunStatus
    metadata: dict[str, Any] = Field(default_factory=dict)


class SimulationRunDetailResponse(SimulationRunResponse):
    snapshot_count: int = 0
    creature_record_count: int = 0
    generation_summary_count: int = 0


class SimulationRunListResponse(BaseModel):
    runs: list[SimulationRunResponse]
    total: int
    limit: int
    offset: int
