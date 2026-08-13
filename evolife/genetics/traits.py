"""Centralized trait definitions and bounds for the genetics subsystem."""

from __future__ import annotations

from dataclasses import dataclass, field
from typing import Iterator


@dataclass(frozen=True)
class TraitDefinition:
    """Metadata for a single genetic trait."""

    name: str
    hard_min: float
    hard_max: float
    generation_min: float
    generation_max: float
    default: float
    mutation_magnitude: float
    description: str = ""

    def __post_init__(self) -> None:
        if self.hard_min > self.hard_max:
            raise ValueError(f"{self.name}: hard_min must be <= hard_max")
        if self.generation_min < self.hard_min or self.generation_max > self.hard_max:
            raise ValueError(f"{self.name}: generation range must lie within hard bounds")
        if not (self.hard_min <= self.default <= self.hard_max):
            raise ValueError(f"{self.name}: default must lie within hard bounds")
        if self.mutation_magnitude <= 0:
            raise ValueError(f"{self.name}: mutation_magnitude must be positive")

    def clamp(self, value: float) -> float:
        """Clamp a value to hard bounds."""
        return max(self.hard_min, min(self.hard_max, value))

    def random_value(self, rng) -> float:
        """Sample a value within the generation range."""
        return rng.uniform(self.generation_min, self.generation_max)


@dataclass
class TraitRegistry:
    """Registry of all genetic traits with centralized bounds."""

    traits: dict[str, TraitDefinition] = field(default_factory=dict)

    def register(self, trait: TraitDefinition) -> None:
        if trait.name in self.traits:
            raise ValueError(f"Trait already registered: {trait.name}")
        self.traits[trait.name] = trait

    def get(self, name: str) -> TraitDefinition:
        if name not in self.traits:
            raise KeyError(f"Unknown trait: {name}")
        return self.traits[name]

    def names(self) -> list[str]:
        return list(self.traits.keys())

    def __iter__(self) -> Iterator[TraitDefinition]:
        return iter(self.traits.values())

    def __len__(self) -> int:
        return len(self.traits)


def default_trait_registry() -> TraitRegistry:
    """Build the default EvoLife trait registry."""
    registry = TraitRegistry()
    registry.register(
        TraitDefinition(
            name="base_movement_speed",
            hard_min=0.5,
            hard_max=5.0,
            generation_min=1.0,
            generation_max=3.0,
            default=2.0,
            mutation_magnitude=0.2,
            description="Baseline locomotion speed (units/sec)",
        )
    )
    registry.register(
        TraitDefinition(
            name="sprint_speed",
            hard_min=1.0,
            hard_max=10.0,
            generation_min=2.0,
            generation_max=6.0,
            default=4.0,
            mutation_magnitude=0.3,
            description="Maximum burst locomotion speed (units/sec)",
        )
    )
    registry.register(
        TraitDefinition(
            name="vision_range",
            hard_min=1.0,
            hard_max=50.0,
            generation_min=5.0,
            generation_max=25.0,
            default=12.0,
            mutation_magnitude=1.5,
            description="Sensory detection radius (units)",
        )
    )
    registry.register(
        TraitDefinition(
            name="maximum_energy",
            hard_min=10.0,
            hard_max=500.0,
            generation_min=50.0,
            generation_max=200.0,
            default=100.0,
            mutation_magnitude=10.0,
            description="Energy capacity ceiling",
        )
    )
    registry.register(
        TraitDefinition(
            name="metabolism_rate",
            hard_min=0.01,
            hard_max=5.0,
            generation_min=0.1,
            generation_max=1.5,
            default=0.5,
            mutation_magnitude=0.05,
            description="Energy consumed per tick (lower = more efficient)",
        )
    )
    registry.register(
        TraitDefinition(
            name="body_size",
            hard_min=0.1,
            hard_max=10.0,
            generation_min=0.5,
            generation_max=3.0,
            default=1.0,
            mutation_magnitude=0.1,
            description="Physical scale affecting collision and energy cost",
        )
    )
    registry.register(
        TraitDefinition(
            name="aggression",
            hard_min=0.0,
            hard_max=1.0,
            generation_min=0.0,
            generation_max=1.0,
            default=0.3,
            mutation_magnitude=0.05,
            description="Tendency toward aggressive behavior (0=passive, 1=aggressive)",
        )
    )
    registry.register(
        TraitDefinition(
            name="reproduction_threshold",
            hard_min=0.1,
            hard_max=1.0,
            generation_min=0.3,
            generation_max=0.9,
            default=0.6,
            mutation_magnitude=0.03,
            description="Fraction of max energy required to reproduce",
        )
    )
    registry.register(
        TraitDefinition(
            name="maximum_age",
            hard_min=10.0,
            hard_max=10000.0,
            generation_min=100.0,
            generation_max=2000.0,
            default=500.0,
            mutation_magnitude=50.0,
            description="Maximum lifespan in simulation ticks (optional trait)",
        )
    )
    return registry
