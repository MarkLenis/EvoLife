"""Lightweight training curriculum stages. Terrain polish is not required."""

from __future__ import annotations

from copy import deepcopy
from enum import Enum

from evolife.experiments.config import (
    MODE_PERSISTENT,
    MODE_TRAINING,
    POLICY_PPO,
    POLICY_SCRIPTED,
    ExperimentConfiguration,
    ExperimentScheduledEvent,
    ExperimentStoppingConditions,
)

STAGE1_MOVEMENT = "stage1_movement"
STAGE2_FOOD_WATER = "stage2_food_water"
STAGE3_PREDATOR_PREY = "stage3_predator_prey"
STAGE4_RESOURCE_SCARCITY = "stage4_resource_scarcity"
STAGE5_PERSISTENT_ECOSYSTEM = "stage5_persistent_ecosystem"
STAGE6_REPRODUCTION_EVENTS = "stage6_reproduction_events"

STAGE_IDS = (
    STAGE1_MOVEMENT,
    STAGE2_FOOD_WATER,
    STAGE3_PREDATOR_PREY,
    STAGE4_RESOURCE_SCARCITY,
    STAGE5_PERSISTENT_ECOSYSTEM,
    STAGE6_REPRODUCTION_EVENTS,
)


class TrainingCurriculumFocus(str, Enum):
    HERBIVORE = "herbivore"
    PREDATOR = "predator"
    COMBINED = "combined"


def create_curriculum_stage(
    stage_id: str | int,
    focus: TrainingCurriculumFocus = TrainingCurriculumFocus.COMBINED,
    baseline: ExperimentConfiguration | None = None,
) -> ExperimentConfiguration:
    if isinstance(stage_id, int):
        stage_id = STAGE_IDS[stage_id - 1]
    if stage_id not in STAGE_IDS:
        raise ValueError(f"unknown curriculum stage '{stage_id}'")
    config = deepcopy(baseline) if baseline is not None else ExperimentConfiguration()
    _training_defaults(config, focus)
    config.curriculum_stage_id = stage_id
    config.scenario_id = stage_id
    _STAGE_APPLY[stage_id](config, focus)
    return config


def _training_defaults(config: ExperimentConfiguration, focus: TrainingCurriculumFocus) -> None:
    config.ecosystem_mode = MODE_TRAINING
    config.training_respawn_enabled = True
    config.training_respawn_interval_seconds = 2.0
    config.apply_stopping(ExperimentStoppingConditions.for_training_episode(180.0))
    config.enabled_environmental_events = []
    config.scheduled_events = []
    config.mutation_probability = 0.0
    config.mutation_magnitude_scale = 0.0
    if focus is TrainingCurriculumFocus.HERBIVORE:
        config.herbivore_policy = POLICY_PPO
        config.predator_policy = POLICY_SCRIPTED
    elif focus is TrainingCurriculumFocus.PREDATOR:
        config.herbivore_policy = POLICY_SCRIPTED
        config.predator_policy = POLICY_PPO
    else:
        config.herbivore_policy = POLICY_PPO
        config.predator_policy = POLICY_PPO


def _stage1(config: ExperimentConfiguration, focus: TrainingCurriculumFocus) -> None:
    config.experiment_name = f"curriculum_{STAGE1_MOVEMENT}_{focus.value}"
    config.resource_abundance = 1.5
    config.plant_regeneration_multiplier = 1.2
    config.day_length_seconds = 90.0
    config.apply_stopping(ExperimentStoppingConditions.for_training_episode(120.0))
    if focus is TrainingCurriculumFocus.PREDATOR:
        config.initial_herbivores = 4
        config.initial_predators = 4
        config.min_herbivores = 2
        config.min_predators = 2
    else:
        config.initial_herbivores = 8
        config.initial_predators = 0
        config.min_herbivores = 4
        config.min_predators = 0
        config.max_predators = 0


def _stage2(config: ExperimentConfiguration, focus: TrainingCurriculumFocus) -> None:
    config.experiment_name = f"curriculum_{STAGE2_FOOD_WATER}_{focus.value}"
    config.resource_abundance = 0.8
    config.plant_regeneration_multiplier = 0.9
    config.apply_stopping(ExperimentStoppingConditions.for_training_episode(180.0))
    if focus is TrainingCurriculumFocus.PREDATOR:
        config.initial_herbivores = 8
        config.initial_predators = 4
        config.min_herbivores = 4
        config.min_predators = 2
    else:
        config.initial_herbivores = 12
        config.initial_predators = 0
        config.min_herbivores = 4
        config.min_predators = 0
        config.max_predators = 0


def _stage3(config: ExperimentConfiguration, focus: TrainingCurriculumFocus) -> None:
    config.experiment_name = f"curriculum_{STAGE3_PREDATOR_PREY}_{focus.value}"
    config.resource_abundance = 1.0
    config.plant_regeneration_multiplier = 1.0
    config.initial_herbivores = 16
    config.initial_predators = 4
    config.min_herbivores = 6
    config.min_predators = 2
    config.apply_stopping(ExperimentStoppingConditions.for_training_episode(240.0))


def _stage4(config: ExperimentConfiguration, focus: TrainingCurriculumFocus) -> None:
    config.experiment_name = f"curriculum_{STAGE4_RESOURCE_SCARCITY}_{focus.value}"
    config.resource_abundance = 0.35
    config.plant_regeneration_multiplier = 0.5
    config.initial_herbivores = 16
    config.initial_predators = 4
    config.min_herbivores = 4
    config.min_predators = 2
    config.apply_stopping(ExperimentStoppingConditions.for_training_episode(240.0))


def _stage5(config: ExperimentConfiguration, focus: TrainingCurriculumFocus) -> None:
    config.experiment_name = f"curriculum_{STAGE5_PERSISTENT_ECOSYSTEM}_{focus.value}"
    config.ecosystem_mode = MODE_PERSISTENT
    config.training_respawn_enabled = False
    config.resource_abundance = 1.0
    config.plant_regeneration_multiplier = 1.0
    config.initial_herbivores = 20
    config.initial_predators = 5
    config.min_herbivores = 0
    config.min_predators = 0
    config.apply_stopping(ExperimentStoppingConditions.for_persistent_ecosystem(600.0))


def _stage6(config: ExperimentConfiguration, focus: TrainingCurriculumFocus) -> None:
    config.experiment_name = f"curriculum_{STAGE6_REPRODUCTION_EVENTS}_{focus.value}"
    config.ecosystem_mode = MODE_PERSISTENT
    config.training_respawn_enabled = False
    config.mutation_probability = 0.15
    config.mutation_magnitude_scale = 1.0
    config.resource_abundance = 1.0
    config.plant_regeneration_multiplier = 1.0
    config.initial_herbivores = 20
    config.initial_predators = 5
    config.min_herbivores = 0
    config.min_predators = 0
    config.enabled_environmental_events = ["drought", "food_boom"]
    config.scheduled_events = [
        ExperimentScheduledEvent(kind="drought", at_simulation_time=120.0),
        ExperimentScheduledEvent(kind="food_boom", at_simulation_time=240.0),
    ]
    config.apply_stopping(ExperimentStoppingConditions.for_persistent_ecosystem(900.0))


_STAGE_APPLY = {
    STAGE1_MOVEMENT: _stage1,
    STAGE2_FOOD_WATER: _stage2,
    STAGE3_PREDATOR_PREY: _stage3,
    STAGE4_RESOURCE_SCARCITY: _stage4,
    STAGE5_PERSISTENT_ECOSYSTEM: _stage5,
    STAGE6_REPRODUCTION_EVENTS: _stage6,
}
