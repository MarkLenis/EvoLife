# Genetics & Evolutionary Inheritance

The genetics subsystem provides genomes, inheritance operations, phenotype decoding, and normalized features for future ML-Agents observations.

## Canonical runtime vs Python reference

| Location | Role |
|----------|------|
| `Assets/EvoLife/Scripts/Genetics/` | **Canonical runtime.** Unity/C# owns live simulation genomes, crossover, mutation, and phenotype decode. The simulation can run genetics with no Python dependency. |
| `evolife/genetics/` | **Offline/reference/research** package. Same conceptual CanonicalGenomeSchema v1 for pytest, analytics, and experimentation. Not imported by Unity. |

Do not treat the two copies as competing runtimes. If the schema changes, update Unity first, then keep the Python registry/bounds in sync.

Per [AGENT_BOUNDARIES.md](AGENT_BOUNDARIES.md):

| Step | Owner |
|------|--------|
| Genome storage, founder generation, crossover, mutation | **Genetics** (`IGeneticOperators`, `Genome`, `CanonicalGenomeSchema`) |
| Phenotype decode | **Genetics** (`IGenomeDecoder`, `Phenotype`, `CreatureGenome`) |
| Apply phenotype to speed / metabolism / senses / energy / age caps | **Creatures** (`CreatureCapabilityMotor`, `CreatureVitals`) |
| Call operators when spawning | **Simulation** (`CreatureSpawner`) — may *call* Genetics, must not define gene layout |
| Consume normalized genetic values | **AI** (future observation source) — must not crossover/mutate |

Creatures and AI must not implement inheritance. Genetics must not own vital drain formulas.

## Canonical Unity genome (schema v1)

`CanonicalGenomeSchema.Version = 1`  
`CanonicalGenomeSchema.TraitCount = 9`

A genome is an ordered set of **named** traits (`TraitId`), not an undocumented float array of length 4. Storage order matches `TraitId` ordinals. Other modules must use `TraitId` / canonical names, never magic indices.

```
TraitId                         canonical name
BaseMovementSpeed               base_movement_speed
SprintSpeed                     sprint_speed
VisionRange                     vision_range
MaximumEnergy                   maximum_energy
MetabolismRate                  metabolism_rate
BodySize                        body_size
Aggression                      aggression
ReproductionThreshold           reproduction_threshold
MaximumAge                      maximum_age
```

Serialized conceptual form (Python `Genome.to_data()` / Unity `Genome.SchemaVersion`):

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

### Trait bounds (hard min/max, founder range, default, mutation magnitude)

| Trait | Hard min | Hard max | Founder min | Founder max | Default | Mutation magnitude |
|-------|----------|----------|-------------|-------------|---------|--------------------|
| `base_movement_speed` | 0.5 | 5.0 | 1.0 | 3.0 | 2.0 | 0.2 |
| `sprint_speed` | 1.0 | 10.0 | 2.0 | 6.0 | 4.0 | 0.3 |
| `vision_range` | 1.0 | 50.0 | 5.0 | 25.0 | 12.0 | 1.5 |
| `maximum_energy` | 10.0 | 500.0 | 50.0 | 200.0 | 100.0 | 10.0 |
| `metabolism_rate` | 0.01 | 5.0 | 0.1 | 1.5 | 0.5 | 0.05 |
| `body_size` | 0.1 | 10.0 | 0.5 | 3.0 | 1.0 | 0.1 |
| `aggression` | 0.0 | 1.0 | 0.0 | 1.0 | 0.3 | 0.05 |
| `reproduction_threshold` | 0.1 | 1.0 | 0.3 | 0.9 | 0.6 | 0.03 |
| `maximum_age` | 10.0 | 10000.0 | 100.0 | 2000.0 | 500.0 | 50.0 |

- **Hard bounds:** every set, crossover, and mutation is clamped. Impossible values cannot be stored.
- **Founder/generation range:** used only by random founder generation (`CreateFounder` / `generate_random_genome`).
- **Mutation magnitude:** max absolute delta per mutation event, scaled by `MutationConfig.MagnitudeScale`.

## Founder generation

Unity:

```csharp
var ops = new DefaultGeneticOperators();
var genome = ops.CreateFounder(new System.Random(seed));
```

Each trait is sampled uniformly in `[generationMin, generationMax]` using the supplied `System.Random`. The same seed produces the same genome. `CreatureSpawner` calls this API; it does not choose trait count or layout.

Python (reference): `generate_random_genome(seed=12345)`.

`Genome.CreateDefault()` fills every trait with its schema default (neutral individual, not a random founder).

## Crossover

Configured by `CrossoverConfig` (default: weighted, `parentAWeight = 0.5`):

| Mode | Behavior |
|------|----------|
| `Average` | Per trait: `(parentA + parentB) / 2` |
| `RandomParent` | Per trait: inherit one parent's value at random |
| `Weighted` (default) | Per trait: `w * parentA + (1-w) * parentB` |

Results are clamped to hard bounds. Null parents fall back to the other parent or a default genome.

## Mutation

Configured by `MutationConfig`:

| Parameter | Default | Description |
|-----------|---------|-------------|
| `Probability` | 0.15 | Per-trait mutation chance |
| `MagnitudeScale` | 1.0 | Multiplier on each trait's `mutationMagnitude` |

For each selected trait, a uniform delta in `[-mag, +mag]` is applied, then **clamped to hard bounds**.

Disable mutation: `GeneticsConfig.NoMutation()` (`probability = 0`).

`CreateOffspring(parentA, parentB, random)` is crossover then mutation using the operator config.

## Genome → Phenotype

```
Genome  --(IGenomeDecoder / CanonicalGenomeDecoder)-->  Phenotype (IReadOnlyPhenotype)
                                                           │
                                                           v
                                                CreatureCapabilityMotor.ApplyPhenotype
                                                           │
                         ┌─────────────────────────────────┼──────────────────────────┐
                         v                                 v                          v
                   locomotion                         metabolism on              sensory range
                   (walk/sprint)                      CreatureVitals             multiplier
                                                      (rates, max energy,
                                                       max age)
```

Decode rule (Unity):

- Multiplier traits: `value / traitDefault` so a default genome is `Phenotype.Neutral` (all multipliers 1).
- `Aggression` is the raw [0, 1] trait, not a multiplier.

Creatures apply locomotion, metabolism, sensory range, max energy, and max age where existing adapters already exist. `reproduction_threshold`, `body_size`, and `aggression` remain on the read-only phenotype for later systems (reproduction / combat). Creatures never read `Genome` internals and never crossover or mutate.

## How future ML-Agents should consume genetic values

Use **normalized [0, 1] features in schema order**, not raw genes and not phenotype multipliers:

```csharp
var vector = GeneticObservationProvider.GetObservationVector(genome);
// length == CanonicalGenomeSchema.TraitCount (9)
// value = (trait - hardMin) / (hardMax - hardMin), clamped to [0, 1]
```

Python equivalent: `GeneticObservationProvider.get_observation_vector(creature)`.

The genetics module has no ML-Agents dependency. AI `CompositeObservationSource` calls this provider (or `Genome.ToNormalizedArray()`) for indices 6–14 of `CreatureObservationSchema` and must not implement operators.

Vital observations are separate: `VitalObservationSource` / the vitals block of `CompositeObservationSource` normalize hunger/thirst using `IReadOnlyVitalState.MaxHunger` / `MaxThirst`.

## Adding a new trait

1. Add `TraitId`, bounds, and default in `CanonicalGenomeSchema` (Unity) and `default_trait_registry()` (Python). Keep names/order aligned.
2. Map it in `CanonicalGenomeDecoder` / `Phenotype` / `IReadOnlyPhenotype` if it should affect capabilities.
3. Wire it in `CreatureCapabilityMotor` / `CreatureVitals` only if a clean adapter already exists.
4. Extend EditMode tests and `tests/genetics/`.
5. Bump `CanonicalGenomeSchema.Version` / `CANONICAL_SCHEMA_VERSION` if the observation vector layout changes.

## API quick reference (Unity)

```csharp
var ops = new DefaultGeneticOperators();
var decoder = new CanonicalGenomeDecoder();

var founder = ops.CreateFounder(new System.Random(42));
var child = ops.CreateOffspring(parentA, parentB, new System.Random(99));
var phenotype = decoder.Decode(child);
var mlVector = GeneticObservationProvider.GetObservationVector(child);
```

## Running tests

Unity EditMode (requires Unity Editor — see [MANUAL_UNITY_VERIFICATION.md](MANUAL_UNITY_VERIFICATION.md)):

- `GeneticOperatorsTests`
- `PhenotypeCapabilityBridgeTests`
- `CreatureBiologyTests` (modifier isolation)
- `VitalObservationSourceTests`

Python reference:

```bash
pip install -e ".[dev]"
pytest tests/genetics/ -v
```
