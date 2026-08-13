"""EvoLife genetics and evolutionary inheritance subsystem."""

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
from evolife.genetics.traits import TraitDefinition, TraitRegistry, default_trait_registry

__all__ = [
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
