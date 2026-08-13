# Genetics & Evolutionary Inheritance

The genetics subsystem provides genomes, inheritance operations, lineage tracking, and integration hooks for simulation and ML policy observations.

## Repository layout

| Location | Role |
|----------|------|
| `evolife/genetics/` | Python reference implementation (pytest, offline ops, analytics) |
| `Assets/EvoLife/Scripts/Genetics/` | Unity runtime seam (`Genome`, `IGeneticOperators`, `Phenotype`) |

Per [AGENT_BOUNDARIES.md](AGENT_BOUNDARIES.md), **Genetics** owns genome data, crossover, mutation, and phenotype decode. The Python package below is the detailed, tested implementation of those operations. Unity integration should call into or mirror these rules via `IGeneticOperators` / `IGenomeDecoder` — not duplicate logic elsewhere.

## Overview

Each creature carries a **genome** — a set of numeric traits with hard bounds. Offspring inherit traits from parents via crossover and mutation. Evolutionary success comes from survival and reproduction, not a global fitness score.

```
Founder genome ──► survive & reproduce ──► offspring genome
                         │
                         ▼
                  CreatureAnalytics (lifetime metrics)
```

## Genome Format

A genome is a mapping of trait name → float value, serialized as:

```json
{
  "version": 1,
  "traits": {
    "base_movement_speed": 2.1,
    "sprint_speed": 4.5,
    "vision_range": 15.0,
    "maximum_energy": 120.0,
    "metabolism_rate": 0.45,
    "body_size": 1.2,
    "aggression": 0.35,
    "reproduction_threshold": 0.6,
    "maximum_age": 480.0
  }
}
```

All trait definitions live in `evolife/genetics/traits.py` via `TraitRegistry`. Bounds are never hardcoded elsewhere.

### Default Traits

| Trait | Hard Min | Hard Max | Description |
|-------|----------|----------|-------------|
| `base_movement_speed` | 0.5 | 5.0 | Baseline locomotion speed |
| `sprint_speed` | 1.0 | 10.0 | Burst locomotion speed |
| `vision_range` | 1.0 | 50.0 | Sensory detection radius |
| `maximum_energy` | 10.0 | 500.0 | Energy capacity |
| `metabolism_rate` | 0.01 | 5.0 | Energy consumed per tick |
| `body_size` | 0.1 | 10.0 | Physical scale |
| `aggression` | 0.0 | 1.0 | Aggressive tendency |
| `reproduction_threshold` | 0.1 | 1.0 | Energy fraction to reproduce |
| `maximum_age` | 10.0 | 10000.0 | Lifespan in ticks (optional) |

Each trait also defines a **generation range** (subset of hard bounds used for random founding genomes) and a **mutation magnitude**.

## Inheritance Pipeline

```
Parent A genome ──┐
                  ├──► Crossover ──► Mutation ──► Offspring genome
Parent B genome ──┘
```

Use `create_offspring(parent_a, parent_b, config, seed)` for full lineage + genome, or `create_offspring_genome(...)` for genome only.

## Crossover

Configured via `CrossoverConfig` with three modes:

| Mode | Behavior |
|------|----------|
| `AVERAGE` | Per trait: `(parent_a + parent_b) / 2` |
| `RANDOM_PARENT` | Per trait: inherit one parent's value at random |
| `WEIGHTED` (default) | Per trait: `w * parent_a + (1-w) * parent_b` |

The default weight is `parent_a_weight = 0.5` (equal blend). All results are clamped to hard bounds.

```python
from evolife.genetics import CrossoverConfig, CrossoverMode, crossover

config = CrossoverConfig(mode=CrossoverMode.WEIGHTED, parent_a_weight=0.6)
child = crossover(parent_a.genome, parent_b.genome, config=config, seed=42)
```

## Mutation

Configured via `MutationConfig`:

| Parameter | Default | Description |
|-----------|---------|-------------|
| `probability` | 0.15 | Per-trait mutation chance |
| `magnitude_scale` | 1.0 | Multiplier on each trait's `mutation_magnitude` |

For each trait selected for mutation, a uniform delta in `[-mag, +mag]` is applied, then the value is **clamped to hard bounds**. Impossible values cannot be produced.

Disable mutation entirely:

```python
from evolife.genetics import GeneticsConfig
config = GeneticsConfig.no_mutation()
```

## Parameter Bounds

- **Hard bounds** (`hard_min`, `hard_max`): absolute limits; clamping always enforces these.
- **Generation range** (`generation_min`, `generation_max`): used by `generate_random_genome`.
- **Mutation magnitude**: max delta per mutation event (before clamping).

## Lineage

`CreatureGenetics` tracks:

| Field | Description |
|-------|-------------|
| `creature_id` | Unique identifier (UUID string) |
| `generation` | 0 for founders; `max(parent generations) + 1` for offspring |
| `parent_ids` | Tuple of 1–2 parent creature IDs |
| `genome` | The creature's genome |
| `analytics` | Lifetime metrics container |

This is intentionally lightweight — not a full genealogy tree.

## Analytics (Not Fitness)

`CreatureAnalytics` records per-creature metrics:

- `lifetime` — ticks survived
- `offspring_count` — successful reproductions
- `food_consumed` — energy from food
- `successful_escapes` — evasions
- `kills` — prey/rivals killed
- `generation_number` — birth generation

These are **not** aggregated into a fitness score. Selection emerges from simulation outcomes.

## Integration with Simulation

Apply genomes to creature parameters via `GenomeConfigAdapter`:

```python
from evolife.genetics import GenomeConfigAdapter

config = GenomeConfigAdapter.from_creature(creature)
# config.base_movement_speed, config.vision_range, etc.
```

The simulation layer reads `CreatureConfig` fields. It does not need to understand genome internals.

## ML Agent Observations

ML agents should consume **normalized genetic features** in `[0, 1]`:

```python
from evolife.genetics import GeneticObservationProvider

vector = GeneticObservationProvider.get_observation_vector(creature)
schema = GeneticObservationProvider.observation_schema(creature)
```

Normalization uses hard bounds: `(value - hard_min) / (hard_max - hard_min)`.

The genetics module has **no dependency** on ML-Agent frameworks. The observation provider is the stable contract.

## Deterministic Generation

All random operations accept an optional `seed`:

```python
g1 = generate_random_genome(seed=12345)
g2 = generate_random_genome(seed=12345)
assert g1.to_dict() == g2.to_dict()
```

`create_offspring(..., seed=N)` derives sub-seeds for crossover and mutation, ensuring full pipeline reproducibility.

## Adding a New Trait

1. **Register in `default_trait_registry()`** (`evolife/genetics/traits.py`):

```python
registry.register(
    TraitDefinition(
        name="camouflage",
        hard_min=0.0,
        hard_max=1.0,
        generation_min=0.0,
        generation_max=0.5,
        default=0.2,
        mutation_magnitude=0.05,
        description="Visual concealment strength",
    )
)
```

2. **Map in `CreatureConfig`** and `GenomeConfigAdapter.from_genome()` (`evolife/genetics/integration.py`).

3. **Add tests** for bounds, mutation, crossover, serialization, and normalization.

Crossover, mutation, serialization, and normalization pick up new traits automatically once registered.

## API Quick Reference

```python
from evolife.genetics import (
    generate_random_genome,
    generate_population,
    create_offspring,
    GeneticsConfig,
    CreatureGenetics,
    GenomeConfigAdapter,
    GeneticObservationProvider,
)

# Founding population
population = generate_population(20, seed=42)

# Reproduction
child = create_offspring(parent_a, parent_b, seed=99)

# Simulation config
creature_config = GenomeConfigAdapter.from_creature(child)

# Policy observations
obs = GeneticObservationProvider.get_observation_vector(child)
```

## Running Tests

```bash
pip install -e ".[dev]"
pytest tests/genetics/ -v
```
