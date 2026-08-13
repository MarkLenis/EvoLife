from typing import Any

from pydantic import BaseModel, Field


class PolicyGroupMetrics(BaseModel):
    policy_kind: str
    creature_count: int = 0
    mean_lifetime: float = 0.0
    mean_generation: float = 0.0
    mean_offspring: float = 0.0
    mean_episode_return: float | None = None
    death_causes: dict[str, int] = Field(default_factory=dict)
    average_genome_traits: dict[str, Any] = Field(default_factory=dict)
    species_counts: dict[str, int] = Field(default_factory=dict)


class PolicyComparisonResponse(BaseModel):
    run_id: str
    groups: list[PolicyGroupMetrics]
    total_creatures: int = 0


class SurvivalRecord(BaseModel):
    creature_id: str
    species: str
    policy_kind: str | None = None
    generation: int
    lifetime: float
    cause_of_death: str | None = None
    birth_time: float
    death_time: float | None = None


class SurvivalRecordsResponse(BaseModel):
    run_id: str
    records: list[SurvivalRecord]
    total: int


class TraitEvolutionPoint(BaseModel):
    generation: int
    species: str
    policy_kind: str | None = None
    population_count: int
    mean: float = 0.0
    variance: float = 0.0


class TraitEvolutionResponse(BaseModel):
    run_id: str
    trait: str
    points: list[TraitEvolutionPoint]
