"""Lifecycle analytics containers — not a global fitness score."""

from __future__ import annotations

from dataclasses import dataclass, field
from typing import Any


@dataclass
class CreatureAnalytics:
    """Per-creature metrics recorded during its lifetime.

    Evolutionary success arises from survival and reproduction, not from
    aggregating these fields into a single fitness value.
    """

    lifetime: float = 0.0
    """Age or ticks survived."""

    offspring_count: int = 0
    """Number of successful reproductions."""

    food_consumed: float = 0.0
    """Total energy gained from food."""

    successful_escapes: int = 0
    """Times the creature evaded a predator or threat."""

    kills: int = 0
    """Prey or rivals killed (where relevant to species)."""

    generation_number: int = 0
    """Generation at birth (copied from lineage)."""

    def record_food(self, amount: float) -> None:
        if amount > 0:
            self.food_consumed += amount

    def record_escape(self) -> None:
        self.successful_escapes += 1

    def record_kill(self) -> None:
        self.kills += 1

    def record_offspring(self, count: int = 1) -> None:
        if count > 0:
            self.offspring_count += count

    def advance_lifetime(self, delta: float) -> None:
        if delta > 0:
            self.lifetime += delta

    def to_dict(self) -> dict[str, Any]:
        return {
            "lifetime": self.lifetime,
            "offspring_count": self.offspring_count,
            "food_consumed": self.food_consumed,
            "successful_escapes": self.successful_escapes,
            "kills": self.kills,
            "generation_number": self.generation_number,
        }

    @classmethod
    def from_dict(cls, data: dict[str, Any]) -> CreatureAnalytics:
        return cls(
            lifetime=data.get("lifetime", 0.0),
            offspring_count=data.get("offspring_count", 0),
            food_consumed=data.get("food_consumed", 0.0),
            successful_escapes=data.get("successful_escapes", 0),
            kills=data.get("kills", 0),
            generation_number=data.get("generation_number", 0),
        )
