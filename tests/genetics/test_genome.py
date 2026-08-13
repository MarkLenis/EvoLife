"""Tests for genome model, serialization, and normalization."""

import pytest

from evolife.genetics import (
    CreatureGenetics,
    Genome,
    default_trait_registry,
    generate_random_genome,
)


@pytest.fixture
def registry():
    return default_trait_registry()


@pytest.fixture
def sample_genome(registry):
    values = {t.name: t.default for t in registry}
    return Genome.from_trait_values(values, registry=registry)


class TestGenomeSerialization:
    def test_all_traits_serialize_correctly(self, sample_genome):
        data = sample_genome.to_data()
        restored = Genome.from_data(data, registry=sample_genome.registry)
        assert restored.to_dict() == sample_genome.to_dict()

    def test_creature_genetics_round_trip(self, sample_genome):
        creature = CreatureGenetics.create_founder(sample_genome, creature_id="founder-1")
        data = creature.to_data()
        restored = CreatureGenetics.from_data(data)
        assert restored.creature_id == "founder-1"
        assert restored.generation == 0
        assert restored.parent_ids == ()
        assert restored.genome.to_dict() == sample_genome.to_dict()

    def test_to_dict_contains_all_traits(self, registry):
        genome = generate_random_genome(registry=registry, seed=42)
        assert set(genome.to_dict().keys()) == set(registry.names())


class TestNormalization:
    def test_normalized_features_in_valid_range(self, registry):
        genome = generate_random_genome(registry=registry, seed=99)
        features = genome.to_normalized_features()
        for name, value in features.items():
            assert 0.0 <= value <= 1.0, f"{name}={value} out of range"

    def test_normalization_at_bounds(self, registry):
        values = {t.name: t.hard_min for t in registry}
        genome = Genome.from_trait_values(values, registry=registry)
        features = genome.to_normalized_features()
        assert all(v == 0.0 for v in features.values())

        values_max = {t.name: t.hard_max for t in registry}
        genome_max = Genome.from_trait_values(values_max, registry=registry)
        features_max = genome_max.to_normalized_features()
        assert all(v == 1.0 for v in features_max.values())

    def test_feature_schema_matches_normalized_keys(self, sample_genome):
        schema = sample_genome.feature_schema()
        features = sample_genome.to_normalized_features()
        assert list(features.keys()) == schema

    def test_clamp_all_enforces_bounds(self, registry):
        genome = generate_random_genome(registry=registry, seed=1)
        # Force out-of-bound value
        trait_name = registry.names()[0]
        genome.traits[trait_name] = -999.0
        clamped = genome.clamp_all()
        td = registry.get(trait_name)
        assert td.hard_min <= clamped.get(trait_name) <= td.hard_max
