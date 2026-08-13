"""Experiment configuration tests runnable without Unity."""

from __future__ import annotations

import json

from pathlib import Path

import pytest

from evolife.experiments.config import (
    ExperimentConfiguration,
    combine_seed,
    derived_seeds,
    evaluate_stop,
    population_rates,
)
from evolife.experiments.curriculum import TrainingCurriculumFocus, create_curriculum_stage
from evolife.experiments.scenarios import SCENARIO_IDS, create_scenario


def test_json_round_trip_preserves_fields():
    original = create_scenario("drought")
    original.model_id = "herbivore_dev"
    original.herbivore_policy = "learned_ppo"
    original.random_seed = 99
    restored = ExperimentConfiguration.from_dict(json.loads(json.dumps(original.to_dict())))
    assert restored.experiment_name == original.experiment_name
    assert restored.random_seed == 99
    assert restored.resource_abundance == original.resource_abundance
    assert restored.enabled_environmental_events == ["drought"]
    assert restored.scheduled_events[0].at_simulation_time == 60.0
    assert restored.model_id == "herbivore_dev"
    assert not restored.validate()


def test_derived_seeds_are_deterministic_and_independent():
    first = derived_seeds(42)
    second = derived_seeds(42)
    other = derived_seeds(43)
    assert first == second
    assert first["founder_genomes"] != other["founder_genomes"]
    assert first["reproduction"] != first["founder_genomes"]
    assert first["resource_spawn"] == combine_seed(42, 29)


def test_scenario_overrides():
    control = create_scenario("normal_control")
    reduced = create_scenario("reduced_food")
    drought = create_scenario("drought")
    fast = create_scenario("fast_predators")
    high = create_scenario("high_mutation")
    low = create_scenario("low_mutation")
    pressure = create_scenario("predator_pressure")
    recovery = create_scenario("recovery_after_event")

    assert set(SCENARIO_IDS) == {
        "normal_control",
        "reduced_food",
        "drought",
        "fast_predators",
        "high_mutation",
        "low_mutation",
        "predator_pressure",
        "recovery_after_event",
    }
    assert reduced.resource_abundance < control.resource_abundance
    assert drought.enabled_environmental_events == ["drought"]
    assert fast.predator_speed_bias > 0
    assert high.mutation_probability > control.mutation_probability
    assert low.mutation_probability < control.mutation_probability
    assert pressure.initial_predators > control.initial_predators
    assert [event.kind for event in recovery.scheduled_events] == ["drought", "food_boom"]


def test_unknown_scenario_is_invalid():
    with pytest.raises(ValueError, match="unknown scenario"):
        create_scenario("not_a_scenario")


def test_stop_conditions_and_manual_stop():
    training = ExperimentConfiguration()
    training.max_simulation_time_seconds = 10
    training.stop_on_ecosystem_extinction = False
    assert evaluate_stop(training, 10, 4, 2) == "max_simulation_time"
    persistent = ExperimentConfiguration()
    assert evaluate_stop(persistent, 5, 0, 0) == "ecosystem_extinct"
    assert evaluate_stop(persistent, 50, 0, 0, manual_stop=True) == "manual_stop"
    assert evaluate_stop(training, 5, 0, 0) == "none"


def test_metadata_dictionary_contains_reproducibility_fields():
    config = create_scenario("drought")
    payload = config.to_dict()
    seeds = derived_seeds(config.random_seed)
    assert payload["scenario_id"] == "drought"
    assert payload["random_seed"] == 42
    assert payload["enabled_environmental_events"] == ["drought"]
    assert seeds["founder_genomes"] == combine_seed(42, 0)


def test_extinct_populations_do_not_divide_by_zero():
    rates = population_rates(0, 0)
    assert rates["herbivore_fraction"] == 0.0
    assert rates["predator_fraction"] == 0.0
    assert rates["predators_per_herbivore"] == 0.0
    assert population_rates(4, 1)["predators_per_herbivore"] == 0.25


def test_policy_selection_by_role_and_curriculum_focus():
    config = ExperimentConfiguration(herbivore_policy="learned_ppo", predator_policy="scripted_baseline")
    assert config.policy_for("herbivore") == "learned_ppo"
    assert config.policy_for("predator") == "scripted_baseline"
    herbivore = create_curriculum_stage(3, TrainingCurriculumFocus.HERBIVORE)
    predator = create_curriculum_stage(3, TrainingCurriculumFocus.PREDATOR)
    combined = create_curriculum_stage(3, TrainingCurriculumFocus.COMBINED)
    assert herbivore.herbivore_policy == "learned_ppo"
    assert herbivore.predator_policy == "scripted_baseline"
    assert predator.predator_policy == "learned_ppo"
    assert combined.herbivore_policy == combined.predator_policy == "learned_ppo"


def test_invalid_configuration_validation():
    config = ExperimentConfiguration(
        experiment_name=" ",
        initial_herbivores=-1,
        mutation_probability=1.5,
        day_length_seconds=0,
        enabled_environmental_events=["tornado"],
        training_respawn_enabled=True,
        ecosystem_mode="persistent_ecosystem",
        herbivore_policy="magic",
    )
    errors = " ".join(config.validate())
    assert "experiment name" in errors
    assert "herbivore count" in errors
    assert "mutation probability" in errors
    assert "day length" in errors
    assert "tornado" in errors
    assert "training respawn" in errors
    assert "herbivore policy" in errors


def test_repo_json_scenarios_and_curriculum_load():
    root = Path(__file__).resolve().parents[2] / "Training" / "experiments"
    for sid in SCENARIO_IDS:
        payload = json.loads((root / "scenarios" / f"{sid}.json").read_text())
        config = ExperimentConfiguration.from_dict(payload)
        assert not config.validate(), config.validate()
        assert config.scenario_id == sid
    for stage in range(1, 7):
        created = create_curriculum_stage(stage, TrainingCurriculumFocus.COMBINED)
        payload = json.loads((root / "curriculum" / f"{created.curriculum_stage_id}_combined.json").read_text())
        loaded = ExperimentConfiguration.from_dict(payload)
        assert loaded.curriculum_stage_id == created.curriculum_stage_id
        assert not loaded.validate(), loaded.validate()


def test_curriculum_stages_are_lightweight_and_valid():
    for stage in range(1, 7):
        config = create_curriculum_stage(stage, TrainingCurriculumFocus.HERBIVORE)
        assert not config.validate(), config.validate()
        assert config.initial_herbivores + config.initial_predators <= 40
    stage1 = create_curriculum_stage(1, TrainingCurriculumFocus.HERBIVORE)
    assert stage1.initial_predators == 0
    stage5 = create_curriculum_stage(5, TrainingCurriculumFocus.COMBINED)
    assert stage5.ecosystem_mode == "persistent_ecosystem"
    assert stage5.training_respawn_enabled is False
    stage6 = create_curriculum_stage(6, TrainingCurriculumFocus.COMBINED)
    assert stage6.mutation_probability > 0
    assert len(stage6.scheduled_events) == 2
