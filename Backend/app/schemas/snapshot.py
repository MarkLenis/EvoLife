from typing import Any

from pydantic import BaseModel, ConfigDict, Field


class ExtensibleModel(BaseModel):
    model_config = ConfigDict(extra="allow")


class SimulationSnapshotCreate(ExtensibleModel):
    simulation_time: float = Field(..., ge=0)
    herbivore_population: int = Field(..., ge=0)
    predator_population: int = Field(..., ge=0)
    plant_count: int = Field(default=0, ge=0)
    births: int = Field(default=0, ge=0)
    deaths: int = Field(default=0, ge=0)
    average_herbivore_speed: float | None = Field(default=None, ge=0)
    average_predator_speed: float | None = Field(default=None, ge=0)
    average_vision: float | None = Field(default=None, ge=0)
    average_lifespan: float | None = Field(default=None, ge=0)
    average_energy: float | None = Field(default=None)
    extra_metrics: dict[str, Any] = Field(default_factory=dict)


class SimulationSnapshotResponse(SimulationSnapshotCreate):
    id: int
    run_id: str


class SimulationSnapshotBatchCreate(BaseModel):
    snapshots: list[SimulationSnapshotCreate] = Field(..., min_length=1)


class SimulationSnapshotBatchResponse(BaseModel):
    inserted: int
    snapshots: list[SimulationSnapshotResponse]


class PopulationTimeSeriesPoint(BaseModel):
    simulation_time: float
    herbivore_population: int
    predator_population: int
    plant_count: int
    births: int
    deaths: int
    total_alive: int = 0
    average_herbivore_speed: float | None = None
    average_predator_speed: float | None = None
    average_vision: float | None = None
    average_lifespan: float | None = None
    average_energy: float | None = None
    extra_metrics: dict[str, Any] = Field(default_factory=dict)


class PopulationTimeSeriesResponse(BaseModel):
    run_id: str
    points: list[PopulationTimeSeriesPoint]
