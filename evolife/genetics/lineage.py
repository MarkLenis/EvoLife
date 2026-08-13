"""Lineage tracking: creature IDs, generation, and parent references."""

from __future__ import annotations

import uuid
from dataclasses import dataclass, field
from typing import Any

from evolife.genetics.analytics import CreatureAnalytics
from evolife.genetics.genome import Genome


CreatureId = str


def _new_creature_id() -> CreatureId:
    return str(uuid.uuid4())


@dataclass
class CreatureGenetics:
    """Genetic identity and lineage for a single creature."""

    creature_id: CreatureId
    generation: int
    parent_ids: tuple[CreatureId, ...]
    genome: Genome
    analytics: CreatureAnalytics = field(default_factory=CreatureAnalytics)

    @classmethod
    def create_founder(
        cls,
        genome: Genome,
        creature_id: CreatureId | None = None,
    ) -> CreatureGenetics:
        cid = creature_id or _new_creature_id()
        analytics = CreatureAnalytics(generation_number=0)
        return cls(
            creature_id=cid,
            generation=0,
            parent_ids=(),
            genome=genome,
            analytics=analytics,
        )

    @classmethod
    def create_offspring(
        cls,
        genome: Genome,
        parent_ids: tuple[CreatureId, ...],
        generation: int,
        creature_id: CreatureId | None = None,
    ) -> CreatureGenetics:
        if len(parent_ids) not in (1, 2):
            raise ValueError("Offspring must have 1 or 2 parent IDs")
        cid = creature_id or _new_creature_id()
        analytics = CreatureAnalytics(generation_number=generation)
        return cls(
            creature_id=cid,
            generation=generation,
            parent_ids=parent_ids,
            genome=genome,
            analytics=analytics,
        )

    def to_data(self) -> dict[str, Any]:
        """Serialize lineage and genome (analytics optional)."""
        return {
            "creature_id": self.creature_id,
            "generation": self.generation,
            "parent_ids": list(self.parent_ids),
            "genome": self.genome.to_data(),
            "analytics": self.analytics.to_dict(),
        }

    @classmethod
    def from_data(cls, data: dict[str, Any], genome: Genome | None = None) -> CreatureGenetics:
        g = genome or Genome.from_data(data["genome"])
        analytics = CreatureAnalytics.from_dict(data.get("analytics", {}))
        if "generation_number" not in data.get("analytics", {}):
            analytics.generation_number = data["generation"]
        return cls(
            creature_id=data["creature_id"],
            generation=data["generation"],
            parent_ids=tuple(data["parent_ids"]),
            genome=g,
            analytics=analytics,
        )
