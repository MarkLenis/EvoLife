"""Canonical schema parity with Unity CanonicalGenomeSchema v1."""

from evolife.genetics import (
    CANONICAL_SCHEMA_VERSION,
    CANONICAL_TRAIT_NAMES,
    GeneticObservationProvider,
    default_trait_registry,
    generate_random_genome,
)


EXPECTED_BOUNDS = {
    "base_movement_speed": (0.5, 5.0, 1.0, 3.0, 2.0, 0.2),
    "sprint_speed": (1.0, 10.0, 2.0, 6.0, 4.0, 0.3),
    "vision_range": (1.0, 50.0, 5.0, 25.0, 12.0, 1.5),
    "maximum_energy": (10.0, 500.0, 50.0, 200.0, 100.0, 10.0),
    "metabolism_rate": (0.01, 5.0, 0.1, 1.5, 0.5, 0.05),
    "body_size": (0.1, 10.0, 0.5, 3.0, 1.0, 0.1),
    "aggression": (0.0, 1.0, 0.0, 1.0, 0.3, 0.05),
    "reproduction_threshold": (0.1, 1.0, 0.3, 0.9, 0.6, 0.03),
    "maximum_age": (10.0, 10000.0, 100.0, 2000.0, 500.0, 50.0),
}


def test_canonical_schema_version_and_trait_count():
    assert CANONICAL_SCHEMA_VERSION == 1
    assert len(CANONICAL_TRAIT_NAMES) == 9
    registry = default_trait_registry()
    assert registry.names() == list(CANONICAL_TRAIT_NAMES)


def test_canonical_trait_bounds_match_unity_schema():
    registry = default_trait_registry()
    for name, expected in EXPECTED_BOUNDS.items():
        trait = registry.get(name)
        hard_min, hard_max, gen_min, gen_max, default, mutation = expected
        assert trait.hard_min == hard_min
        assert trait.hard_max == hard_max
        assert trait.generation_min == gen_min
        assert trait.generation_max == gen_max
        assert trait.default == default
        assert trait.mutation_magnitude == mutation


def test_observation_schema_uses_canonical_order():
    from evolife.genetics import CreatureGenetics

    creature = CreatureGenetics.create_founder(
        generate_random_genome(seed=55), creature_id="schema"
    )
    schema = GeneticObservationProvider.observation_schema(creature)
    assert schema == list(CANONICAL_TRAIT_NAMES)
