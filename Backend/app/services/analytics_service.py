from sqlalchemy.orm import Session

from app.persistence.models import (
    CreatureLifeRecordModel,
    GenerationSummaryModel,
    SimulationRunModel,
    SimulationSnapshotModel,
)
from app.persistence.repositories import (
    CreatureRepository,
    GenerationRepository,
    RunNotFoundError,
    RunRepository,
    SnapshotRepository,
)
from app.schemas.creature import (
    CreatureLifeRecordCreate,
    CreatureLifeRecordListResponse,
    CreatureLifeRecordResponse,
)
from app.schemas.evaluation import (
    PolicyComparisonResponse,
    PolicyGroupMetrics,
    SurvivalRecord,
    SurvivalRecordsResponse,
    TraitEvolutionPoint,
    TraitEvolutionResponse,
)
from app.schemas.generation import (
    EvolutionTimeSeriesPoint,
    EvolutionTimeSeriesResponse,
    GenerationSummaryCreate,
    GenerationSummaryListResponse,
    GenerationSummaryResponse,
)
from app.schemas.run import (
    SimulationRunCreate,
    SimulationRunDetailResponse,
    SimulationRunFinish,
    SimulationRunListResponse,
    SimulationRunResponse,
)
from app.schemas.snapshot import (
    PopulationTimeSeriesPoint,
    PopulationTimeSeriesResponse,
    SimulationSnapshotCreate,
    SimulationSnapshotResponse,
)


class AnalyticsService:
    def __init__(self, session: Session) -> None:
        self.session = session
        self.runs = RunRepository(session)
        self.snapshots = SnapshotRepository(session)
        self.creatures = CreatureRepository(session)
        self.generations = GenerationRepository(session)

    def create_run(self, payload: SimulationRunCreate) -> SimulationRunResponse:
        extra = payload.model_extra or {}
        run = SimulationRunModel(
            experiment_name=payload.experiment_name,
            random_seed=payload.random_seed,
            configuration=payload.configuration,
            status=payload.status.value,
            metadata_json={**payload.metadata, **extra},
        )
        created = self.runs.create(run)
        return self._run_to_response(created)

    def finish_run(self, run_id: str, payload: SimulationRunFinish) -> SimulationRunResponse:
        extra = payload.model_extra or {}
        run = self.runs.finish(
            run_id,
            status=payload.status.value,
            finished_at=payload.finished_at,
        )
        if extra:
            run.metadata_json = {**run.metadata_json, **extra}
            self.session.commit()
            self.session.refresh(run)
        return self._run_to_response(run)

    def get_run(self, run_id: str) -> SimulationRunDetailResponse:
        run = self.runs.get_with_details(run_id)
        if run is None:
            raise RunNotFoundError(run_id)
        response = self._run_to_response(run)
        return SimulationRunDetailResponse(
            **response.model_dump(),
            snapshot_count=len(run.snapshots),
            creature_record_count=len(run.creatures),
            generation_summary_count=len(run.generations),
        )

    def list_runs(
        self,
        *,
        experiment_name: str | None = None,
        status: str | None = None,
        limit: int = 50,
        offset: int = 0,
    ) -> SimulationRunListResponse:
        runs = self.runs.list_runs(
            experiment_name=experiment_name,
            status=status,
            limit=limit,
            offset=offset,
        )
        return SimulationRunListResponse(
            runs=[self._run_to_response(run) for run in runs],
            total=len(runs),
            limit=limit,
            offset=offset,
        )

    def add_snapshot(self, run_id: str, payload: SimulationSnapshotCreate) -> SimulationSnapshotResponse:
        self.runs.get_by_id_or_raise(run_id)
        snapshot = self._snapshot_from_payload(run_id, payload)
        created = self.snapshots.add(snapshot)
        return self._snapshot_to_response(created)

    def add_snapshots_batch(
        self,
        run_id: str,
        payloads: list[SimulationSnapshotCreate],
    ) -> list[SimulationSnapshotResponse]:
        self.runs.get_by_id_or_raise(run_id)
        snapshots = [self._snapshot_from_payload(run_id, payload) for payload in payloads]
        created = self.snapshots.add_many(snapshots)
        return [self._snapshot_to_response(item) for item in created]

    def add_creature_records(
        self,
        run_id: str,
        payloads: list[CreatureLifeRecordCreate],
    ) -> list[CreatureLifeRecordResponse]:
        self.runs.get_by_id_or_raise(run_id)
        records = [self._creature_from_payload(run_id, payload) for payload in payloads]
        created = self.creatures.add_many(records)
        return [self._creature_to_response(item) for item in created]

    def add_generation_summaries(
        self,
        run_id: str,
        payloads: list[GenerationSummaryCreate],
    ) -> list[GenerationSummaryResponse]:
        self.runs.get_by_id_or_raise(run_id)
        summaries = [self._generation_from_payload(run_id, payload) for payload in payloads]
        created = self.generations.add_many(summaries)
        return [self._generation_to_response(item) for item in created]

    def get_population_time_series(self, run_id: str) -> PopulationTimeSeriesResponse:
        self.runs.get_by_id_or_raise(run_id)
        snapshots = self.snapshots.list_for_run(run_id)
        points = [
            PopulationTimeSeriesPoint(
                simulation_time=snapshot.simulation_time,
                herbivore_population=snapshot.herbivore_population,
                predator_population=snapshot.predator_population,
                plant_count=snapshot.plant_count,
                births=snapshot.births,
                deaths=snapshot.deaths,
                total_alive=_snapshot_total_alive(snapshot),
                average_herbivore_speed=snapshot.average_herbivore_speed,
                average_predator_speed=snapshot.average_predator_speed,
                average_vision=snapshot.average_vision,
                average_lifespan=snapshot.average_lifespan,
                average_energy=snapshot.average_energy,
                extra_metrics=snapshot.extra_metrics,
            )
            for snapshot in snapshots
        ]
        return PopulationTimeSeriesResponse(run_id=run_id, points=points)

    def get_evolution_time_series(
        self,
        run_id: str,
        *,
        species: str | None = None,
    ) -> EvolutionTimeSeriesResponse:
        self.runs.get_by_id_or_raise(run_id)
        summaries = self.generations.list_for_run(run_id, species=species)
        points = [
            EvolutionTimeSeriesPoint(
                species=summary.species,
                generation=summary.generation,
                population_count=summary.population_count,
                average_genome_traits=summary.average_genome_traits,
                average_lifespan=summary.average_lifespan,
                reproduction_rate=summary.reproduction_rate,
                offspring_per_parent=summary.offspring_per_parent,
                extra_statistics=summary.extra_statistics,
            )
            for summary in summaries
        ]
        return EvolutionTimeSeriesResponse(run_id=run_id, points=points)

    def list_creature_records(
        self,
        run_id: str,
        *,
        species: str | None = None,
        generation: int | None = None,
        policy_kind: str | None = None,
        cause_of_death: str | None = None,
    ) -> CreatureLifeRecordListResponse:
        self.runs.get_by_id_or_raise(run_id)
        records = self.creatures.list_for_run(
            run_id,
            species=species,
            generation=generation,
            policy_kind=policy_kind,
            cause_of_death=cause_of_death,
        )
        return CreatureLifeRecordListResponse(
            run_id=run_id,
            records=[self._creature_to_response(item) for item in records],
            total=len(records),
        )

    def list_generation_summaries(
        self,
        run_id: str,
        *,
        species: str | None = None,
    ) -> GenerationSummaryListResponse:
        self.runs.get_by_id_or_raise(run_id)
        summaries = self.generations.list_for_run(run_id, species=species)
        return GenerationSummaryListResponse(
            run_id=run_id,
            summaries=[self._generation_to_response(item) for item in summaries],
            total=len(summaries),
        )

    def get_policy_comparison(self, run_id: str) -> PolicyComparisonResponse:
        self.runs.get_by_id_or_raise(run_id)
        records = self.creatures.list_for_run(run_id)
        grouped: dict[str, list[CreatureLifeRecordModel]] = {}
        for record in records:
            key = record.policy_kind or (record.extra_fields or {}).get("policy_kind") or "unspecified"
            grouped.setdefault(str(key), []).append(record)

        groups = [_policy_group_metrics(kind, items) for kind, items in sorted(grouped.items())]
        return PolicyComparisonResponse(run_id=run_id, groups=groups, total_creatures=len(records))

    def get_survival_records(
        self,
        run_id: str,
        *,
        policy_kind: str | None = None,
        species: str | None = None,
    ) -> SurvivalRecordsResponse:
        self.runs.get_by_id_or_raise(run_id)
        records = self.creatures.list_for_run(run_id, species=species, policy_kind=policy_kind)
        survival = [
            SurvivalRecord(
                creature_id=record.creature_id,
                species=record.species,
                policy_kind=record.policy_kind,
                generation=record.generation,
                lifetime=_creature_lifetime(record),
                cause_of_death=record.cause_of_death,
                birth_time=record.birth_time,
                death_time=record.death_time,
            )
            for record in records
        ]
        return SurvivalRecordsResponse(run_id=run_id, records=survival, total=len(survival))

    def get_trait_evolution(
        self,
        run_id: str,
        trait: str,
        *,
        species: str | None = None,
        policy_kind: str | None = None,
    ) -> TraitEvolutionResponse:
        self.runs.get_by_id_or_raise(run_id)
        summaries = self.generations.list_for_run(run_id, species=species)
        points: list[TraitEvolutionPoint] = []
        for summary in summaries:
            extra = summary.extra_statistics or {}
            if policy_kind:
                by_policy = extra.get("by_policy") or {}
                slice_stats = by_policy.get(policy_kind) if isinstance(by_policy, dict) else None
                if not isinstance(slice_stats, dict):
                    continue
                traits = slice_stats.get("average_genome_traits") or {}
                variance = (slice_stats.get("trait_variance") or {}).get(trait, 0.0)
                population = int(slice_stats.get("population_count") or 0)
            else:
                traits = summary.average_genome_traits or {}
                variance = (extra.get("trait_variance") or {}).get(trait, 0.0)
                population = summary.population_count

            if trait not in traits:
                continue
            try:
                mean = float(traits[trait])
                variance_value = float(variance or 0.0)
            except (TypeError, ValueError):
                continue
            points.append(
                TraitEvolutionPoint(
                    generation=summary.generation,
                    species=summary.species,
                    policy_kind=policy_kind,
                    population_count=population,
                    mean=mean,
                    variance=variance_value,
                )
            )
        return TraitEvolutionResponse(run_id=run_id, trait=trait, points=points)

    @staticmethod
    def _run_to_response(run: SimulationRunModel) -> SimulationRunResponse:
        return SimulationRunResponse(
            run_id=run.run_id,
            started_at=run.started_at,
            finished_at=run.finished_at,
            experiment_name=run.experiment_name,
            random_seed=run.random_seed,
            configuration=run.configuration,
            status=run.status,
            metadata=run.metadata_json,
        )

    @staticmethod
    def _snapshot_from_payload(run_id: str, payload: SimulationSnapshotCreate) -> SimulationSnapshotModel:
        extra = payload.model_extra or {}
        metrics = {**payload.extra_metrics, **extra}
        return SimulationSnapshotModel(
            run_id=run_id,
            simulation_time=payload.simulation_time,
            herbivore_population=payload.herbivore_population,
            predator_population=payload.predator_population,
            plant_count=payload.plant_count,
            births=payload.births,
            deaths=payload.deaths,
            average_herbivore_speed=payload.average_herbivore_speed,
            average_predator_speed=payload.average_predator_speed,
            average_vision=payload.average_vision,
            average_lifespan=payload.average_lifespan,
            average_energy=payload.average_energy,
            extra_metrics=metrics,
        )

    @staticmethod
    def _snapshot_to_response(snapshot: SimulationSnapshotModel) -> SimulationSnapshotResponse:
        return SimulationSnapshotResponse(
            id=snapshot.id,
            run_id=snapshot.run_id,
            simulation_time=snapshot.simulation_time,
            herbivore_population=snapshot.herbivore_population,
            predator_population=snapshot.predator_population,
            plant_count=snapshot.plant_count,
            births=snapshot.births,
            deaths=snapshot.deaths,
            average_herbivore_speed=snapshot.average_herbivore_speed,
            average_predator_speed=snapshot.average_predator_speed,
            average_vision=snapshot.average_vision,
            average_lifespan=snapshot.average_lifespan,
            average_energy=snapshot.average_energy,
            extra_metrics=snapshot.extra_metrics,
        )

    @staticmethod
    def _creature_from_payload(run_id: str, payload: CreatureLifeRecordCreate) -> CreatureLifeRecordModel:
        extra = payload.model_extra or {}
        fields = {**payload.extra_fields, **extra}
        policy_kind = payload.policy_kind or fields.get("policy_kind")
        if policy_kind is not None:
            policy_kind = str(policy_kind)
            fields.setdefault("policy_kind", policy_kind)
        return CreatureLifeRecordModel(
            run_id=run_id,
            creature_id=payload.creature_id,
            species=payload.species,
            generation=payload.generation,
            birth_time=payload.birth_time,
            death_time=payload.death_time,
            cause_of_death=payload.cause_of_death,
            parent_id_1=payload.parent_id_1,
            parent_id_2=payload.parent_id_2,
            offspring_count=payload.offspring_count,
            genome_traits=payload.genome_traits,
            policy_kind=policy_kind,
            extra_fields=fields,
        )

    @staticmethod
    def _creature_to_response(record: CreatureLifeRecordModel) -> CreatureLifeRecordResponse:
        return CreatureLifeRecordResponse(
            id=record.id,
            run_id=record.run_id,
            creature_id=record.creature_id,
            species=record.species,
            generation=record.generation,
            birth_time=record.birth_time,
            death_time=record.death_time,
            cause_of_death=record.cause_of_death,
            parent_id_1=record.parent_id_1,
            parent_id_2=record.parent_id_2,
            offspring_count=record.offspring_count,
            genome_traits=record.genome_traits,
            policy_kind=record.policy_kind,
            extra_fields=record.extra_fields,
        )

    @staticmethod
    def _generation_from_payload(run_id: str, payload: GenerationSummaryCreate) -> GenerationSummaryModel:
        extra = payload.model_extra or {}
        stats = {**payload.extra_statistics, **extra}
        return GenerationSummaryModel(
            run_id=run_id,
            species=payload.species,
            generation=payload.generation,
            population_count=payload.population_count,
            average_genome_traits=payload.average_genome_traits,
            average_lifespan=payload.average_lifespan,
            reproduction_rate=payload.reproduction_rate,
            offspring_per_parent=payload.offspring_per_parent,
            notes=payload.notes,
            extra_statistics=stats,
        )

    @staticmethod
    def _generation_to_response(summary: GenerationSummaryModel) -> GenerationSummaryResponse:
        return GenerationSummaryResponse(
            id=summary.id,
            run_id=summary.run_id,
            species=summary.species,
            generation=summary.generation,
            population_count=summary.population_count,
            average_genome_traits=summary.average_genome_traits,
            average_lifespan=summary.average_lifespan,
            reproduction_rate=summary.reproduction_rate,
            offspring_per_parent=summary.offspring_per_parent,
            notes=summary.notes,
            extra_statistics=summary.extra_statistics,
        )


def _snapshot_total_alive(snapshot: SimulationSnapshotModel) -> int:
    metrics = snapshot.extra_metrics or {}
    total = metrics.get("totalAlive")
    if total is None:
        return snapshot.herbivore_population + snapshot.predator_population
    try:
        return int(total)
    except (TypeError, ValueError):
        return snapshot.herbivore_population + snapshot.predator_population


def _creature_lifetime(record: CreatureLifeRecordModel) -> float:
    extra = record.extra_fields or {}
    if extra.get("lifetime") is not None:
        try:
            return max(0.0, float(extra["lifetime"]))
        except (TypeError, ValueError):
            pass
    if record.death_time is not None:
        return max(0.0, record.death_time - record.birth_time)
    return 0.0


def _mean(values: list[float]) -> float:
    if not values:
        return 0.0
    return sum(values) / len(values)


def _policy_group_metrics(policy_kind: str, records: list[CreatureLifeRecordModel]) -> PolicyGroupMetrics:
    lifetimes = [_creature_lifetime(record) for record in records]
    generations = [float(record.generation) for record in records]
    offspring = [float(record.offspring_count) for record in records]
    returns: list[float] = []
    death_causes: dict[str, int] = {}
    species_counts: dict[str, int] = {}
    trait_values: dict[str, list[float]] = {}

    for record in records:
        extra = record.extra_fields or {}
        if extra.get("episode_return") is not None:
            try:
                returns.append(float(extra["episode_return"]))
            except (TypeError, ValueError):
                pass
        cause = record.cause_of_death or "unknown"
        death_causes[cause] = death_causes.get(cause, 0) + 1
        species_counts[record.species] = species_counts.get(record.species, 0) + 1
        for name, value in (record.genome_traits or {}).items():
            try:
                trait_values.setdefault(str(name), []).append(float(value))
            except (TypeError, ValueError):
                continue

    return PolicyGroupMetrics(
        policy_kind=policy_kind,
        creature_count=len(records),
        mean_lifetime=_mean(lifetimes),
        mean_generation=_mean(generations),
        mean_offspring=_mean(offspring),
        mean_episode_return=_mean(returns) if returns else None,
        death_causes=death_causes,
        average_genome_traits={name: _mean(values) for name, values in trait_values.items()},
        species_counts=species_counts,
    )
