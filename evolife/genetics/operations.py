"""Genetic operations: random generation, crossover, mutation."""

from __future__ import annotations

import random
from typing import Sequence

from evolife.genetics.config import CrossoverConfig, CrossoverMode, GeneticsConfig
from evolife.genetics.genome import Genome
from evolife.genetics.lineage import CreatureGenetics, CreatureId
from evolife.genetics.traits import TraitRegistry, default_trait_registry


def _make_rng(seed: int | None) -> random.Random:
    return random.Random(seed)


def generate_random_genome(
    registry: TraitRegistry | None = None,
    seed: int | None = None,
) -> Genome:
    """Create a genome with random trait values within generation ranges."""
    registry = registry or default_trait_registry()
    rng = _make_rng(seed)
    values = {trait.name: trait.random_value(rng) for trait in registry}
    return Genome.from_trait_values(values, registry=registry)


def crossover(
    parent_a: Genome,
    parent_b: Genome,
    config: CrossoverConfig | None = None,
    seed: int | None = None,
) -> Genome:
    """Combine two parent genomes using the configured crossover strategy.

    Supported modes:
    - AVERAGE: arithmetic mean of both parents per trait
    - RANDOM_PARENT: per trait, inherit one parent's value at random
    - WEIGHTED: linear interpolation; weight controlled by parent_a_weight
    """
    config = config or CrossoverConfig()
    if parent_a.registry is not parent_b.registry:
        raise ValueError("Parent genomes must share the same trait registry")

    registry = parent_a.registry
    rng = _make_rng(seed)
    offspring_values: dict[str, float] = {}

    for trait_def in registry:
        a_val = parent_a.get(trait_def.name)
        b_val = parent_b.get(trait_def.name)

        if config.mode == CrossoverMode.AVERAGE:
            combined = (a_val + b_val) / 2.0
        elif config.mode == CrossoverMode.RANDOM_PARENT:
            combined = a_val if rng.random() < 0.5 else b_val
        elif config.mode == CrossoverMode.WEIGHTED:
            w = config.parent_a_weight
            combined = w * a_val + (1.0 - w) * b_val
        else:
            raise ValueError(f"Unknown crossover mode: {config.mode}")

        offspring_values[trait_def.name] = trait_def.clamp(combined)

    return Genome.from_trait_values(offspring_values, registry=registry)


def mutate(
    genome: Genome,
    config: GeneticsConfig | None = None,
    seed: int | None = None,
) -> Genome:
    """Apply per-trait mutation with configurable probability and magnitude.

    Each trait mutates independently with `mutation.probability`.
    Mutation delta is drawn uniformly from [-mag, +mag] where mag is
    trait.mutation_magnitude * mutation.magnitude_scale.
    Results are clamped to hard bounds — impossible values cannot occur.
    """
    config = config or GeneticsConfig()
    rng = _make_rng(seed)
    registry = genome.registry
    mutated = dict(genome.traits)

    for trait_def in registry:
        if rng.random() >= config.mutation.probability:
            continue
        magnitude = trait_def.mutation_magnitude * config.mutation.magnitude_scale
        if magnitude == 0:
            continue
        delta = rng.uniform(-magnitude, magnitude)
        new_value = trait_def.clamp(mutated[trait_def.name] + delta)
        mutated[trait_def.name] = new_value

    return Genome.from_trait_values(mutated, registry=registry)


def create_offspring_genome(
    parent_a: Genome,
    parent_b: Genome,
    config: GeneticsConfig | None = None,
    seed: int | None = None,
) -> Genome:
    """Full inheritance pipeline: crossover then mutation."""
    config = config or GeneticsConfig()
    rng = _make_rng(seed)
    # Derive sub-seeds for reproducibility when a master seed is given
    if seed is not None:
        crossover_seed = rng.randint(0, 2**31 - 1)
        mutation_seed = rng.randint(0, 2**31 - 1)
    else:
        crossover_seed = None
        mutation_seed = None

    child = crossover(parent_a, parent_b, config.crossover, seed=crossover_seed)
    return mutate(child, config, seed=mutation_seed)


def create_offspring(
    parent_a: CreatureGenetics,
    parent_b: CreatureGenetics,
    config: GeneticsConfig | None = None,
    seed: int | None = None,
    creature_id: CreatureId | None = None,
) -> CreatureGenetics:
    """Create offspring with inherited genome and lineage metadata."""
    child_genome = create_offspring_genome(
        parent_a.genome, parent_b.genome, config=config, seed=seed
    )
    generation = max(parent_a.generation, parent_b.generation) + 1
    return CreatureGenetics.create_offspring(
        genome=child_genome,
        parent_ids=(parent_a.creature_id, parent_b.creature_id),
        generation=generation,
        creature_id=creature_id,
    )


def generate_population(
    count: int,
    registry: TraitRegistry | None = None,
    seed: int | None = None,
) -> list[CreatureGenetics]:
    """Generate a founding population of generation-0 creatures."""
    registry = registry or default_trait_registry()
    rng = _make_rng(seed)
    population: list[CreatureGenetics] = []
    for _ in range(count):
        individual_seed = rng.randint(0, 2**31 - 1) if seed is not None else None
        genome = generate_random_genome(registry=registry, seed=individual_seed)
        population.append(CreatureGenetics.create_founder(genome))
    return population
