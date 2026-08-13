from typing import Any

from pydantic import BaseModel, ConfigDict, Field


class ExtensibleModel(BaseModel):
    model_config = ConfigDict(extra="allow")


class GenerationSummaryCreate(ExtensibleModel):
    species: str = Field(..., min_length=1, max_length=64)
    generation: int = Field(..., ge=0)
    population_count: int = Field(..., ge=0)
    average_genome_traits: dict[str, Any] = Field(default_factory=dict)
    average_lifespan: float | None = Field(default=None, ge=0)
    reproduction_rate: float | None = Field(default=None, ge=0)
    offspring_per_parent: float | None = Field(default=None, ge=0)
    notes: str | None = None
    extra_statistics: dict[str, Any] = Field(default_factory=dict)


class GenerationSummaryResponse(GenerationSummaryCreate):
    id: int
    run_id: str


class GenerationSummaryBatchCreate(BaseModel):
    summaries: list[GenerationSummaryCreate] = Field(..., min_length=1)


class GenerationSummaryBatchResponse(BaseModel):
    inserted: int
    summaries: list[GenerationSummaryResponse]


class EvolutionTimeSeriesPoint(BaseModel):
    species: str
    generation: int
    population_count: int
    average_genome_traits: dict[str, Any] = Field(default_factory=dict)
    average_lifespan: float | None = None
    reproduction_rate: float | None = None
    offspring_per_parent: float | None = None
    extra_statistics: dict[str, Any] = Field(default_factory=dict)


class EvolutionTimeSeriesResponse(BaseModel):
    run_id: str
    points: list[EvolutionTimeSeriesPoint]
