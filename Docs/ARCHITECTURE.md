# EvoLife Architecture

EvoLife is an AI-driven wildlife and ecosystem simulator. Individual animals share learned species policies while each animal possesses genetically inherited traits.

## Subsystems

| Subsystem | Responsibility |
|-----------|----------------|
| **Genetics** | Genome model, inheritance, crossover, mutation, lineage |
| **Simulation** | World state, physics, energy, creature lifecycle |
| **Policy / AI** | Shared species neural policy; per-creature genetic observations |
| **Analytics** | Lifetime metrics, reproduction outcomes, population tracking |

## Design Principles

1. **Decoupled modules** — Genetics does not depend on ML-Agent frameworks. Integration happens through adapters and normalized feature vectors.
2. **Centralized configuration** — Trait bounds, mutation rates, and crossover behavior live in config, not scattered literals.
3. **Evolution by survival** — No global fitness score drives selection. Creatures that survive and reproduce propagate genomes.
4. **Deterministic reproducibility** — Seeded random generation supports testing and experiment replay.

## Data Flow

```
Parent genomes ──► Crossover ──► Mutation ──► Offspring genome
                                                    │
                                                    ▼
                                          CreatureGenetics (lineage)
                                                    │
                                    ┌───────────────┴───────────────┐
                                    ▼                               ▼
                          CreatureConfigAdapter            Normalized features
                          (simulation params)                (policy observations)
```

## Out of Scope (Current Phase)

- Reinforcement learning / PPO training
- Visual rendering
- Mating behavior implementation
- FastAPI / web services
- Environmental event system
