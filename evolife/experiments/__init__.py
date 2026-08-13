"""Offline experiment configuration for EvoLife.

Unity ``ExperimentConfiguration`` is the runtime source of truth. This package
mirrors the JSON document so scenarios, seeds, and stop rules can be checked
without the Unity Editor.
"""

from evolife.experiments.config import (
    ExperimentConfiguration,
    ExperimentScheduledEvent,
    ExperimentStoppingConditions,
    combine_seed,
    derived_seeds,
)
from evolife.experiments.curriculum import TrainingCurriculumFocus, create_curriculum_stage
from evolife.experiments.scenarios import SCENARIO_IDS, create_scenario

__all__ = [
    "SCENARIO_IDS",
    "ExperimentConfiguration",
    "ExperimentScheduledEvent",
    "ExperimentStoppingConditions",
    "TrainingCurriculumFocus",
    "combine_seed",
    "create_curriculum_stage",
    "create_scenario",
    "derived_seeds",
]
