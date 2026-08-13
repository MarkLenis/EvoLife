# Agent Boundaries

This document defines ownership and integration contracts between EvoLife subsystems.

## Genetics Agent

**Owns:**
- Trait definitions and parameter bounds
- Genome data model and serialization
- Crossover, mutation, random generation
- Lineage identifiers (creature ID, generation, parent IDs)
- Analytics data structures (lifetime metrics, not fitness scores)
- Genome → creature configuration adapter
- Normalized genetic feature export for observations

**Does NOT own:**
- Neural policy training or RL algorithms
- Creature movement, rendering, or combat logic
- Mating pair selection or reproduction timing
- World/environment simulation

## Integration Contracts

### Simulation → Genetics

Simulation requests offspring genomes via `create_offspring(parent_a, parent_b, config, seed)`. It applies resulting traits through `GenomeConfigAdapter`, not by reading raw genome internals.

### Policy / AI → Genetics

ML agents consume **normalized genetic features** via `Genome.to_normalized_features()`. The genetics module exposes a stable feature schema; it never imports ML-Agent classes.

### Analytics → Genetics

Simulation records lifecycle events into `CreatureAnalytics`. Genetics provides the container; simulation fills it during the creature's life.

## Adding a New Trait

1. Register the trait in `TraitRegistry` with bounds and generation range.
2. Genome serialization and normalization pick it up automatically.
3. Map the trait to a simulation parameter in `GenomeConfigAdapter`.
4. Add tests for bounds, mutation, crossover, and serialization.

See `Docs/GENETICS.md` for detailed instructions.
