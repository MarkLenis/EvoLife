from typing import Any

from pydantic import BaseModel, ConfigDict, Field


class ExtensibleModel(BaseModel):
    model_config = ConfigDict(extra="allow")


class CreatureLifeRecordCreate(ExtensibleModel):
    creature_id: str = Field(..., min_length=1, max_length=64)
    species: str = Field(..., min_length=1, max_length=64)
    generation: int = Field(..., ge=0)
    birth_time: float = Field(..., ge=0)
    death_time: float | None = Field(default=None, ge=0)
    cause_of_death: str | None = Field(default=None, max_length=128)
    parent_id_1: str | None = Field(default=None, max_length=64)
    parent_id_2: str | None = Field(default=None, max_length=64)
    offspring_count: int = Field(default=0, ge=0)
    genome_traits: dict[str, Any] = Field(default_factory=dict)
    extra_fields: dict[str, Any] = Field(default_factory=dict)


class CreatureLifeRecordResponse(CreatureLifeRecordCreate):
    id: int
    run_id: str


class CreatureLifeRecordBatchCreate(BaseModel):
    records: list[CreatureLifeRecordCreate] = Field(..., min_length=1)


class CreatureLifeRecordBatchResponse(BaseModel):
    inserted: int
    records: list[CreatureLifeRecordResponse]
