"""Configuration for genetic operations."""

from __future__ import annotations

from dataclasses import dataclass
from enum import Enum


class CrossoverMode(str, Enum):
    """Strategy for combining parent trait values during crossover."""

    AVERAGE = "average"
    RANDOM_PARENT = "random_parent"
    WEIGHTED = "weighted"


@dataclass(frozen=True)
class CrossoverConfig:
    """Controls how parent genomes are combined."""

    mode: CrossoverMode = CrossoverMode.WEIGHTED
    parent_a_weight: float = 0.5
    """Weight for parent A when mode is WEIGHTED (parent B gets 1 - weight)."""

    def __post_init__(self) -> None:
        if not (0.0 <= self.parent_a_weight <= 1.0):
            raise ValueError("parent_a_weight must be in [0, 1]")


@dataclass(frozen=True)
class MutationConfig:
    """Controls random mutation of offspring genomes."""

    probability: float = 0.15
    """Per-trait probability of mutation."""

    magnitude_scale: float = 1.0
    """Multiplier applied to each trait's configured mutation_magnitude."""

    def __post_init__(self) -> None:
        if not (0.0 <= self.probability <= 1.0):
            raise ValueError("probability must be in [0, 1]")
        if self.magnitude_scale < 0:
            raise ValueError("magnitude_scale must be non-negative")


@dataclass(frozen=True)
class GeneticsConfig:
    """Top-level genetics configuration."""

    crossover: CrossoverConfig = CrossoverConfig()
    mutation: MutationConfig = MutationConfig()

    @classmethod
    def no_mutation(cls) -> GeneticsConfig:
        """Config with mutation disabled (for stable crossover tests)."""
        return cls(mutation=MutationConfig(probability=0.0, magnitude_scale=0.0))
