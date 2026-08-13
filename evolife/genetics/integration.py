"""Integration layer: apply genomes to creature configuration without ML coupling."""

from __future__ import annotations

from dataclasses import dataclass
from typing import Any

from evolife.genetics.genome import Genome
from evolife.genetics.lineage import CreatureGenetics


@dataclass(frozen=True)
class CreatureConfig:
    """Simulation-facing parameters derived from a genome.

    This is the contract between genetics and the simulation layer.
    ML-Agent code should not import this directly for observations;
    use Genome.to_normalized_features() instead.
    """

    base_movement_speed: float
    sprint_speed: float
    vision_range: float
    maximum_energy: float
    metabolism_rate: float
    body_size: float
    aggression: float
    reproduction_threshold: float
    maximum_age: float

    def to_dict(self) -> dict[str, float]:
        return {
            "base_movement_speed": self.base_movement_speed,
            "sprint_speed": self.sprint_speed,
            "vision_range": self.vision_range,
            "maximum_energy": self.maximum_energy,
            "metabolism_rate": self.metabolism_rate,
            "body_size": self.body_size,
            "aggression": self.aggression,
            "reproduction_threshold": self.reproduction_threshold,
            "maximum_age": self.maximum_age,
        }


class GenomeConfigAdapter:
    """Maps genomes to creature configuration parameters."""

    @staticmethod
    def from_genome(genome: Genome) -> CreatureConfig:
        """Build creature config from genome trait values."""
        g = genome.get
        return CreatureConfig(
            base_movement_speed=g("base_movement_speed"),
            sprint_speed=g("sprint_speed"),
            vision_range=g("vision_range"),
            maximum_energy=g("maximum_energy"),
            metabolism_rate=g("metabolism_rate"),
            body_size=g("body_size"),
            aggression=g("aggression"),
            reproduction_threshold=g("reproduction_threshold"),
            maximum_age=g("maximum_age"),
        )

    @staticmethod
    def from_creature(creature: CreatureGenetics) -> CreatureConfig:
        return GenomeConfigAdapter.from_genome(creature.genome)


class GeneticObservationProvider:
    """Stable interface for ML agents to query normalized genetic features."""

    @staticmethod
    def get_observation_vector(creature: CreatureGenetics) -> list[float]:
        """Return normalized features in schema order for policy input."""
        features = creature.genome.to_normalized_features()
        schema = creature.genome.feature_schema()
        return [features[name] for name in schema]

    @staticmethod
    def get_observation_dict(creature: CreatureGenetics) -> dict[str, float]:
        return creature.genome.to_normalized_features()

    @staticmethod
    def observation_size(creature: CreatureGenetics) -> int:
        return len(creature.genome.feature_schema())

    @staticmethod
    def observation_schema(creature: CreatureGenetics) -> list[str]:
        return creature.genome.feature_schema()
