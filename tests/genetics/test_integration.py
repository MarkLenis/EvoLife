"""Tests for integration layer and analytics."""

import pytest

from evolife.genetics import (
    CreatureAnalytics,
    CreatureGenetics,
    GeneticObservationProvider,
    GenomeConfigAdapter,
    default_trait_registry,
    generate_random_genome,
)


@pytest.fixture
def creature():
    registry = default_trait_registry()
    genome = generate_random_genome(registry=registry, seed=55)
    return CreatureGenetics.create_founder(genome, creature_id="test-creature")


class TestGenomeConfigAdapter:
    def test_maps_all_config_fields(self, creature):
        config = GenomeConfigAdapter.from_creature(creature)
        d = config.to_dict()
        assert set(d.keys()) == set(creature.genome.to_dict().keys())
        for key, val in creature.genome.to_dict().items():
            assert d[key] == val

    def test_config_values_match_genome(self, creature):
        config = GenomeConfigAdapter.from_genome(creature.genome)
        assert config.base_movement_speed == creature.genome.get("base_movement_speed")
        assert config.maximum_age == creature.genome.get("maximum_age")


class TestGeneticObservationProvider:
    def test_observation_vector_size_matches_schema(self, creature):
        schema = GeneticObservationProvider.observation_schema(creature)
        vector = GeneticObservationProvider.get_observation_vector(creature)
        assert len(vector) == len(schema)
        assert GeneticObservationProvider.observation_size(creature) == len(schema)

    def test_observation_values_normalized(self, creature):
        vector = GeneticObservationProvider.get_observation_vector(creature)
        assert all(0.0 <= v <= 1.0 for v in vector)

    def test_observation_dict_matches_vector(self, creature):
        schema = GeneticObservationProvider.observation_schema(creature)
        d = GeneticObservationProvider.get_observation_dict(creature)
        vector = GeneticObservationProvider.get_observation_vector(creature)
        assert [d[k] for k in schema] == vector


class TestCreatureAnalytics:
    def test_record_methods(self):
        a = CreatureAnalytics(generation_number=2)
        a.advance_lifetime(10.0)
        a.record_food(5.0)
        a.record_escape()
        a.record_kill()
        a.record_offspring(2)

        assert a.lifetime == 10.0
        assert a.food_consumed == 5.0
        assert a.successful_escapes == 1
        assert a.kills == 1
        assert a.offspring_count == 2
        assert a.generation_number == 2

    def test_analytics_serialization(self):
        a = CreatureAnalytics(generation_number=3, lifetime=100.0, offspring_count=1)
        restored = CreatureAnalytics.from_dict(a.to_dict())
        assert restored.lifetime == 100.0
        assert restored.offspring_count == 1
        assert restored.generation_number == 3
