"""Starter evaluation scenarios. These change knobs only; they do not claim outcomes."""

from __future__ import annotations

from copy import deepcopy

from evolife.experiments.config import (
    ExperimentConfiguration,
    ExperimentScheduledEvent,
    ExperimentStoppingConditions,
)

NORMAL_CONTROL = "normal_control"
REDUCED_FOOD = "reduced_food"
DROUGHT = "drought"
FAST_PREDATORS = "fast_predators"
HIGH_MUTATION = "high_mutation"
LOW_MUTATION = "low_mutation"
PREDATOR_PRESSURE = "predator_pressure"
RECOVERY_AFTER_EVENT = "recovery_after_event"

SCENARIO_IDS = (
    NORMAL_CONTROL,
    REDUCED_FOOD,
    DROUGHT,
    FAST_PREDATORS,
    HIGH_MUTATION,
    LOW_MUTATION,
    PREDATOR_PRESSURE,
    RECOVERY_AFTER_EVENT,
)


def create_scenario(scenario_id: str, baseline: ExperimentConfiguration | None = None) -> ExperimentConfiguration:
    if scenario_id not in SCENARIO_IDS:
        raise ValueError(f"unknown scenario id '{scenario_id}'")
    config = deepcopy(baseline) if baseline is not None else ExperimentConfiguration()
    config.scenario_id = scenario_id
    _APPLY[scenario_id](config)
    return config


def _normal_control(config: ExperimentConfiguration) -> None:
    config.experiment_name = NORMAL_CONTROL
    config.resource_abundance = 1.0
    config.plant_regeneration_multiplier = 1.0
    config.mutation_probability = 0.15
    config.mutation_magnitude_scale = 1.0
    config.enabled_environmental_events = []
    config.scheduled_events = []
    config.predator_speed_bias = 0.0
    config.ecosystem_mode = "persistent_ecosystem"
    config.training_respawn_enabled = False
    config.apply_stopping(ExperimentStoppingConditions.for_persistent_ecosystem(600.0))


def _reduced_food(config: ExperimentConfiguration) -> None:
    _normal_control(config)
    config.experiment_name = REDUCED_FOOD
    config.scenario_id = REDUCED_FOOD
    config.resource_abundance = 0.35
    config.plant_regeneration_multiplier = 0.7


def _drought(config: ExperimentConfiguration) -> None:
    _normal_control(config)
    config.experiment_name = DROUGHT
    config.scenario_id = DROUGHT
    config.resource_abundance = 0.7
    config.plant_regeneration_multiplier = 0.4
    config.enabled_environmental_events = ["drought"]
    config.scheduled_events = [ExperimentScheduledEvent(kind="drought", at_simulation_time=60.0)]


def _fast_predators(config: ExperimentConfiguration) -> None:
    _normal_control(config)
    config.experiment_name = FAST_PREDATORS
    config.scenario_id = FAST_PREDATORS
    config.predator_speed_bias = 1.2
    config.initial_predators = max(config.initial_predators, 7)


def _high_mutation(config: ExperimentConfiguration) -> None:
    _normal_control(config)
    config.experiment_name = HIGH_MUTATION
    config.scenario_id = HIGH_MUTATION
    config.mutation_probability = 0.45
    config.mutation_magnitude_scale = 2.5


def _low_mutation(config: ExperimentConfiguration) -> None:
    _normal_control(config)
    config.experiment_name = LOW_MUTATION
    config.scenario_id = LOW_MUTATION
    config.mutation_probability = 0.02
    config.mutation_magnitude_scale = 0.25


def _predator_pressure(config: ExperimentConfiguration) -> None:
    _normal_control(config)
    config.experiment_name = PREDATOR_PRESSURE
    config.scenario_id = PREDATOR_PRESSURE
    config.initial_herbivores = 16
    config.initial_predators = 12
    config.max_predators = max(config.max_predators, 32)


def _recovery_after_event(config: ExperimentConfiguration) -> None:
    _normal_control(config)
    config.experiment_name = RECOVERY_AFTER_EVENT
    config.scenario_id = RECOVERY_AFTER_EVENT
    config.enabled_environmental_events = ["drought", "food_boom"]
    config.scheduled_events = [
        ExperimentScheduledEvent(kind="drought", at_simulation_time=40.0),
        ExperimentScheduledEvent(kind="food_boom", at_simulation_time=100.0),
    ]
    config.apply_stopping(ExperimentStoppingConditions.for_persistent_ecosystem(300.0))


_APPLY = {
    NORMAL_CONTROL: _normal_control,
    REDUCED_FOOD: _reduced_food,
    DROUGHT: _drought,
    FAST_PREDATORS: _fast_predators,
    HIGH_MUTATION: _high_mutation,
    LOW_MUTATION: _low_mutation,
    PREDATOR_PRESSURE: _predator_pressure,
    RECOVERY_AFTER_EVENT: _recovery_after_event,
}
