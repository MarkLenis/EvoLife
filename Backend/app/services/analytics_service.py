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
from app.schemas.creature import CreatureLifeRecordCreate, CreatureLifeRecordResponse
from app.schemas.generation import (
    EvolutionTimeSeriesPoint,
    EvolutionTimeSeriesResponse,
    GenerationSummaryCreate,
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
