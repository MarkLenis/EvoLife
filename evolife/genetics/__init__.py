"""EvoLife genetics — offline/reference implementation of CanonicalGenomeSchema v1.

The Unity runtime (Assets/EvoLife/Scripts/Genetics/) is the canonical simulation
implementation. This package mirrors the same trait schema for research, analytics,
and pytest coverage. Unity does not import or depend on this Python package.
"""

from evolife.genetics.analytics import CreatureAnalytics
from evolife.genetics.config import (
    CrossoverConfig,
    CrossoverMode,
    GeneticsConfig,
    MutationConfig,
)
from evolife.genetics.genome import Genome
from evolife.genetics.integration import (
    CreatureConfig,
    GeneticObservationProvider,
    GenomeConfigAdapter,
)
from evolife.genetics.lineage import CreatureGenetics, CreatureId
from evolife.genetics.operations import (
    create_offspring,
    create_offspring_genome,
    crossover,
    generate_population,
    generate_random_genome,
    mutate,
)
from evolife.genetics.traits import (
    CANONICAL_SCHEMA_VERSION,
    CANONICAL_TRAIT_NAMES,
    TraitDefinition,
    TraitRegistry,
    default_trait_registry,
)

__all__ = [
    "CANONICAL_SCHEMA_VERSION",
    "CANONICAL_TRAIT_NAMES",
    "CreatureAnalytics",
    "CreatureConfig",
    "CreatureGenetics",
    "CreatureId",
    "CrossoverConfig",
    "CrossoverMode",
    "GeneticObservationProvider",
    "GeneticsConfig",
    "Genome",
    "GenomeConfigAdapter",
    "MutationConfig",
    "TraitDefinition",
    "TraitRegistry",
    "create_offspring",
    "create_offspring_genome",
    "crossover",
    "default_trait_registry",
    "generate_population",
    "generate_random_genome",
    "mutate",
]
