"""Serializable experiment configuration matching Unity ExperimentConfiguration JSON."""

from __future__ import annotations

from dataclasses import asdict, dataclass, field
from typing import Any

POLICY_SCRIPTED = "scripted_baseline"
POLICY_PPO = "learned_ppo"
MODE_PERSISTENT = "persistent_ecosystem"
MODE_TRAINING = "training_support"

KNOWN_POLICIES = {POLICY_SCRIPTED, POLICY_PPO, "ScriptedBaseline", "LearnedPpo"}
KNOWN_MODES = {MODE_PERSISTENT, MODE_TRAINING, "Persistent", "TrainingSupport"}
KNOWN_EVENTS = {
    "drought",
    "wildfire",
    "heat_wave",
    "food_boom",
    "disease_pressure",
    "predator_introduction",
    "predator_removal",
}

FOUNDER_OFFSET = 0
REPRODUCTION_OFFSET = 17
RESOURCE_OFFSET = 29
EVENT_OFFSET = 41
WANDER_OFFSET = 59
RESPAWN_OFFSET = 31
ENVIRONMENTAL_CREATURES_OFFSET = 53


def to_i32(value: int) -> int:
    value &= 0xFFFFFFFF
    return value - 0x100000000 if value >= 0x80000000 else value


def combine_seed(master_seed: int, offset: int) -> int:
    """Match Unity ``DeterministicSeeds.Combine`` (unchecked 32-bit)."""
    return to_i32(to_i32(master_seed * 397) ^ offset)


def derived_seeds(master_seed: int) -> dict[str, int]:
    return {
        "master": master_seed,
        "founder_genomes": combine_seed(master_seed, FOUNDER_OFFSET),
        "reproduction": combine_seed(master_seed, REPRODUCTION_OFFSET),
        "resource_spawn": combine_seed(master_seed, RESOURCE_OFFSET),
        "event_schedule": combine_seed(master_seed, EVENT_OFFSET),
        "training_respawn": combine_seed(master_seed, RESPAWN_OFFSET),
        "environmental_creatures": combine_seed(master_seed, ENVIRONMENTAL_CREATURES_OFFSET),
        "scripted_wander": combine_seed(master_seed, WANDER_OFFSET),
    }


@dataclass
class ExperimentScheduledEvent:
    kind: str
    at_simulation_time: float = 0.0


@dataclass
class ExperimentStoppingConditions:
    max_simulation_time_seconds: float = 600.0
    stop_on_ecosystem_extinction: bool = True
    stop_on_herbivore_extinction: bool = False
    stop_on_predator_extinction: bool = False

    @classmethod
    def for_training_episode(cls, max_simulation_time_seconds: float) -> ExperimentStoppingConditions:
        return cls(
            max_simulation_time_seconds=max_simulation_time_seconds,
            stop_on_ecosystem_extinction=False,
            stop_on_herbivore_extinction=False,
            stop_on_predator_extinction=False,
        )

    @classmethod
    def for_persistent_ecosystem(cls, max_simulation_time_seconds: float) -> ExperimentStoppingConditions:
        return cls(
            max_simulation_time_seconds=max_simulation_time_seconds,
            stop_on_ecosystem_extinction=True,
        )


@dataclass
class ExperimentConfiguration:
    experiment_name: str = "baseline"
    random_seed: int = 42
    initial_herbivores: int = 20
    initial_predators: int = 5
    resource_abundance: float = 1.0
    plant_regeneration_multiplier: float = 1.0
    mutation_probability: float = 0.15
    mutation_magnitude_scale: float = 1.0
    day_length_seconds: float = 120.0
    enabled_environmental_events: list[str] = field(default_factory=list)
    scheduled_events: list[ExperimentScheduledEvent] = field(default_factory=list)
    herbivore_policy: str = POLICY_SCRIPTED
    predator_policy: str = POLICY_SCRIPTED
    max_herbivores: int = 80
    max_predators: int = 24
    min_herbivores: int = 4
    min_predators: int = 2
    ecosystem_mode: str = MODE_PERSISTENT
    training_respawn_enabled: bool = False
    training_respawn_interval_seconds: float = 2.0
    founder_spawn_radius: float = 12.0
    default_time_scale: float = 1.0
    scenario_id: str = ""
    model_id: str = ""
    curriculum_stage_id: str = ""
    predator_speed_bias: float = 0.0
    max_simulation_time_seconds: float = 600.0
    stop_on_ecosystem_extinction: bool = True
    stop_on_herbivore_extinction: bool = False
    stop_on_predator_extinction: bool = False

    def policy_for(self, role: str) -> str:
        if role == "predator":
            return self.predator_policy
        if role == "herbivore":
            return self.herbivore_policy
        raise ValueError(f"unhandled role {role}")

    def validate(self) -> list[str]:
        errors: list[str] = []
        if not self.experiment_name or not self.experiment_name.strip():
            errors.append("experiment name is required.")
        if self.initial_herbivores < 0:
            errors.append("initial herbivore count must be >= 0.")
        if self.initial_predators < 0:
            errors.append("initial predator count must be >= 0.")
        if self.resource_abundance < 0:
            errors.append("resource abundance must be >= 0.")
        if self.plant_regeneration_multiplier < 0:
            errors.append("plant regeneration multiplier must be >= 0.")
        if not 0.0 <= self.mutation_probability <= 1.0:
            errors.append("mutation probability must be in [0, 1].")
        if self.mutation_magnitude_scale < 0:
            errors.append("mutation magnitude scale must be >= 0.")
        if self.day_length_seconds <= 0:
            errors.append("day length must be > 0.")
        if self.max_herbivores < 0 or self.max_predators < 0:
            errors.append("population caps must be >= 0.")
        if self.max_herbivores > 0 and self.initial_herbivores > self.max_herbivores:
            errors.append("initial herbivores must be <= max herbivores.")
        if self.max_predators > 0 and self.initial_predators > self.max_predators:
            errors.append("initial predators must be <= max predators.")
        if self.training_respawn_enabled and self.ecosystem_mode not in {MODE_TRAINING, "TrainingSupport"}:
            errors.append("training respawn requires ecosystem mode training_support.")
        if self.herbivore_policy not in KNOWN_POLICIES:
            errors.append(f"invalid herbivore policy '{self.herbivore_policy}'.")
        if self.predator_policy not in KNOWN_POLICIES:
            errors.append(f"invalid predator policy '{self.predator_policy}'.")
        if self.ecosystem_mode not in KNOWN_MODES:
            errors.append(f"invalid ecosystem mode '{self.ecosystem_mode}'.")
        for event in self.enabled_environmental_events:
            if event not in KNOWN_EVENTS:
                errors.append(f"unknown enabled environmental event '{event}'.")
        for event in self.scheduled_events:
            if event.kind not in KNOWN_EVENTS:
                errors.append(f"unknown scheduled environmental event '{event.kind}'.")
        return errors

    def to_dict(self) -> dict[str, Any]:
        payload = asdict(self)
        payload["scheduled_events"] = [
            {"kind": event.kind, "at_simulation_time": event.at_simulation_time}
            for event in self.scheduled_events
        ]
        return payload

    @classmethod
    def from_dict(cls, payload: dict[str, Any]) -> ExperimentConfiguration:
        data = dict(payload)
        scheduled = [
            ExperimentScheduledEvent(
                kind=item["kind"],
                at_simulation_time=float(item.get("at_simulation_time", 0.0)),
            )
            for item in data.pop("scheduled_events", []) or []
        ]
        known = {field.name for field in cls.__dataclass_fields__.values()}
        filtered = {key: value for key, value in data.items() if key in known}
        config = cls(**filtered)
        config.scheduled_events = scheduled
        return config

    def apply_stopping(self, stopping: ExperimentStoppingConditions) -> None:
        self.max_simulation_time_seconds = stopping.max_simulation_time_seconds
        self.stop_on_ecosystem_extinction = stopping.stop_on_ecosystem_extinction
        self.stop_on_herbivore_extinction = stopping.stop_on_herbivore_extinction
        self.stop_on_predator_extinction = stopping.stop_on_predator_extinction


def evaluate_stop(
    config: ExperimentConfiguration,
    simulation_time: float,
    herbivores: int,
    predators: int,
    manual_stop: bool = False,
) -> str:
    if manual_stop:
        return "manual_stop"
    ecosystem_extinct = herbivores <= 0 and predators <= 0
    if config.stop_on_ecosystem_extinction and ecosystem_extinct:
        return "ecosystem_extinct"
    if config.stop_on_herbivore_extinction and herbivores <= 0:
        return "herbivores_extinct"
    if config.stop_on_predator_extinction and predators <= 0:
        return "predators_extinct"
    if config.max_simulation_time_seconds > 0 and simulation_time >= config.max_simulation_time_seconds:
        return "max_simulation_time"
    return "none"


def population_rates(herbivores: int, predators: int) -> dict[str, float]:
    total = herbivores + predators
    return {
        "herbivore_fraction": 0.0 if total <= 0 else herbivores / total,
        "predator_fraction": 0.0 if total <= 0 else predators / total,
        "predators_per_herbivore": 0.0 if herbivores <= 0 else predators / herbivores,
    }
