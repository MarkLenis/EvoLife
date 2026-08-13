"""Tests for crossover, mutation, and seeded generation."""

import pytest

from evolife.genetics import (
    CreatureGenetics,
    CrossoverConfig,
    CrossoverMode,
    GeneticsConfig,
    Genome,
    MutationConfig,
    create_offspring,
    create_offspring_genome,
    crossover,
    default_trait_registry,
    generate_population,
    generate_random_genome,
    mutate,
)

@pytest.fixture
def registry():
    return default_trait_registry()


def _genome_at_extremes(registry, use_max: bool) -> Genome:
    values = {
        t.name: (t.hard_max if use_max else t.hard_min) for t in registry
    }
    return Genome.from_trait_values(values, registry=registry)


class TestSeededGeneration:
    def test_seeded_random_genome_is_reproducible(self, registry):
        g1 = generate_random_genome(registry=registry, seed=12345)
        g2 = generate_random_genome(registry=registry, seed=12345)
        assert g1.to_dict() == g2.to_dict()

    def test_different_seeds_differ(self, registry):
        g1 = generate_random_genome(registry=registry, seed=1)
        g2 = generate_random_genome(registry=registry, seed=2)
        assert g1.to_dict() != g2.to_dict()

    def test_population_seeded_reproducible(self, registry):
        p1 = generate_population(5, registry=registry, seed=777)
        p2 = generate_population(5, registry=registry, seed=777)
        for a, b in zip(p1, p2):
            assert a.genome.to_dict() == b.genome.to_dict()


class TestCrossover:
    def test_average_crossover_within_parent_range(self, registry):
        parent_a = _genome_at_extremes(registry, use_max=False)
        parent_b = _genome_at_extremes(registry, use_max=True)
        config = CrossoverConfig(mode=CrossoverMode.AVERAGE)
        child = crossover(parent_a, parent_b, config=config, seed=0)

        for trait_def in registry:
            val = child.get(trait_def.name)
            assert trait_def.hard_min <= val <= trait_def.hard_max
            a_val = parent_a.get(trait_def.name)
            b_val = parent_b.get(trait_def.name)
            lo, hi = min(a_val, b_val), max(a_val, b_val)
            assert lo <= val <= hi

    def test_random_parent_crossover_inherits_parent_value(self, registry):
        parent_a = _genome_at_extremes(registry, use_max=False)
        parent_b = _genome_at_extremes(registry, use_max=True)
        config = CrossoverConfig(mode=CrossoverMode.RANDOM_PARENT)

        for seed in range(20):
            child = crossover(parent_a, parent_b, config=config, seed=seed)
            for trait_def in registry:
                val = child.get(trait_def.name)
                a_val = parent_a.get(trait_def.name)
                b_val = parent_b.get(trait_def.name)
                assert val in (a_val, b_val)

    def test_weighted_crossover_respects_bounds(self, registry):
        parent_a = _genome_at_extremes(registry, use_max=False)
        parent_b = _genome_at_extremes(registry, use_max=True)
        config = CrossoverConfig(mode=CrossoverMode.WEIGHTED, parent_a_weight=0.75)
        child = crossover(parent_a, parent_b, config=config, seed=42)

        for trait_def in registry:
            val = child.get(trait_def.name)
            assert trait_def.hard_min <= val <= trait_def.hard_max

    def test_crossover_seeded_reproducible(self, registry):
        parent_a = generate_random_genome(registry=registry, seed=1)
        parent_b = generate_random_genome(registry=registry, seed=2)
        c1 = crossover(parent_a, parent_b, seed=999)
        c2 = crossover(parent_a, parent_b, seed=999)
        assert c1.to_dict() == c2.to_dict()


class TestMutation:
    def test_mutation_respects_hard_bounds(self, registry):
        genome = generate_random_genome(registry=registry, seed=5)
        config = GeneticsConfig(
            mutation=MutationConfig(probability=1.0, magnitude_scale=10.0)
        )
        for seed in range(50):
            mutated = mutate(genome, config=config, seed=seed)
            mutated.validate()

    def test_zero_mutation_produces_stable_result(self, registry):
        parent_a = generate_random_genome(registry=registry, seed=10)
        parent_b = generate_random_genome(registry=registry, seed=20)
        config = GeneticsConfig.no_mutation()
        child = create_offspring_genome(parent_a, parent_b, config=config, seed=100)
        child2 = create_offspring_genome(parent_a, parent_b, config=config, seed=100)
        assert child.to_dict() == child2.to_dict()

    def test_mutation_probability_zero_never_changes(self, registry):
        genome = generate_random_genome(registry=registry, seed=3)
        config = GeneticsConfig(mutation=MutationConfig(probability=0.0))
        for seed in range(30):
            assert mutate(genome, config=config, seed=seed).to_dict() == genome.to_dict()

    def test_mutation_probability_affects_change_rate(self, registry):
        """With p=1.0, nearly all runs should differ; with p=0.0, none do."""
        genome = generate_random_genome(registry=registry, seed=4)
        high_config = GeneticsConfig(
            mutation=MutationConfig(probability=1.0, magnitude_scale=1.0)
        )
        changes = sum(
            1
            for s in range(100)
            if mutate(genome, config=high_config, seed=s).to_dict() != genome.to_dict()
        )
        assert changes > 80  # expect most traits to mutate at p=1.0

        zero_config = GeneticsConfig(mutation=MutationConfig(probability=0.0))
        no_changes = sum(
            1
            for s in range(100)
            if mutate(genome, config=zero_config, seed=s).to_dict() != genome.to_dict()
        )
        assert no_changes == 0


class TestOffspring:
    def test_offspring_records_parents_and_generation(self, registry):
        g1 = generate_random_genome(registry=registry, seed=1)
        g2 = generate_random_genome(registry=registry, seed=2)
        parent_a = CreatureGenetics.create_founder(g1, creature_id="parent-a")
        parent_b = CreatureGenetics.create_founder(g2, creature_id="parent-b")

        child = create_offspring(parent_a, parent_b, seed=42, creature_id="child-1")
        assert child.creature_id == "child-1"
        assert child.generation == 1
        assert set(child.parent_ids) == {"parent-a", "parent-b"}
        assert child.analytics.generation_number == 1

    def test_generation_increments_from_max_parent(self, registry):
        g = generate_random_genome(registry=registry, seed=0)
        p0 = CreatureGenetics.create_founder(g, creature_id="p0")
        p5 = CreatureGenetics(
            creature_id="p5",
            generation=5,
            parent_ids=("x",),
            genome=g,
        )
        child = create_offspring(p0, p5, seed=1, creature_id="c")
        assert child.generation == 6

    def test_offspring_genome_seeded_reproducible(self, registry):
        g1 = generate_random_genome(registry=registry, seed=1)
        g2 = generate_random_genome(registry=registry, seed=2)
        pa = CreatureGenetics.create_founder(g1)
        pb = CreatureGenetics.create_founder(g2)
        c1 = create_offspring(pa, pb, seed=999)
        c2 = create_offspring(pa, pb, seed=999)
        assert c1.genome.to_dict() == c2.genome.to_dict()
