"""Genome data model with serialization and normalization."""

from __future__ import annotations

from dataclasses import dataclass, field
from typing import Any

from evolife.genetics.traits import TraitDefinition, TraitRegistry, default_trait_registry


@dataclass
class Genome:
    """A creature's genetic trait values."""

    traits: dict[str, float] = field(default_factory=dict)
    registry: TraitRegistry = field(default_factory=default_trait_registry, repr=False)

    def __post_init__(self) -> None:
        for name, value in list(self.traits.items()):
            self.traits[name] = self.registry.get(name).clamp(value)

    @classmethod
    def from_trait_values(
        cls,
        values: dict[str, float],
        registry: TraitRegistry | None = None,
    ) -> Genome:
        registry = registry or default_trait_registry()
        clamped = {name: registry.get(name).clamp(val) for name, val in values.items()}
        return cls(traits=clamped, registry=registry)

    def get(self, name: str) -> float:
        if name not in self.traits:
            raise KeyError(f"Trait not in genome: {name}")
        return self.traits[name]

    def clamp_all(self) -> Genome:
        """Return a new genome with all traits clamped to hard bounds."""
        clamped = {
            name: self.registry.get(name).clamp(value)
            for name, value in self.traits.items()
        }
        return Genome(traits=clamped, registry=self.registry)

    def to_dict(self) -> dict[str, float]:
        """Serialize trait values as a plain dict."""
        return dict(self.traits)

    def to_data(self) -> dict[str, Any]:
        """Full serialization including schema version."""
        return {
            "version": 1,
            "traits": self.to_dict(),
        }

    @classmethod
    def from_data(
        cls,
        data: dict[str, Any],
        registry: TraitRegistry | None = None,
    ) -> Genome:
        registry = registry or default_trait_registry()
        if "traits" not in data:
            raise ValueError("Genome data must contain 'traits' key")
        return cls.from_trait_values(data["traits"], registry=registry)

    def to_normalized_features(self) -> dict[str, float]:
        """Map each trait to [0, 1] using hard bounds for ML observations."""
        features: dict[str, float] = {}
        for trait_def in self.registry:
            if trait_def.name not in self.traits:
                continue
            value = self.traits[trait_def.name]
            span = trait_def.hard_max - trait_def.hard_min
            if span == 0:
                features[trait_def.name] = 0.0
            else:
                normalized = (value - trait_def.hard_min) / span
                features[trait_def.name] = max(0.0, min(1.0, normalized))
        return features

    def feature_schema(self) -> list[str]:
        """Ordered list of trait names in normalized feature output."""
        return sorted(
            name
            for name in self.registry.names()
            if name in self.traits
        )

    def validate(self) -> None:
        """Ensure all registered traits are present and within bounds."""
        for trait_def in self.registry:
            if trait_def.name not in self.traits:
                raise ValueError(f"Missing trait: {trait_def.name}")
            value = self.traits[trait_def.name]
            if not (trait_def.hard_min <= value <= trait_def.hard_max):
                raise ValueError(
                    f"Trait {trait_def.name}={value} outside "
                    f"[{trait_def.hard_min}, {trait_def.hard_max}]"
                )

    def _trait_span(self, trait_def: TraitDefinition) -> float:
        return trait_def.hard_max - trait_def.hard_min
