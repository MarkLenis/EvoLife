import uuid
from datetime import datetime, timezone

from sqlalchemy.orm import Session

from app.persistence.models import SimulationRunModel, SimulationSnapshotModel
from app.persistence.repositories import RunRepository, SnapshotRepository
from app.schemas.v1 import ExperimentCreate, ExperimentRecord, StatsSnapshotIn


class V1Service:
    """Unity-compatible v1 experiment and stats API backed by shared persistence."""

    def __init__(self, session: Session) -> None:
        self.session = session
        self.runs = RunRepository(session)
        self.snapshots = SnapshotRepository(session)

    def create_experiment(self, payload: ExperimentCreate) -> ExperimentRecord:
        run_id = str(uuid.uuid4())
        run = SimulationRunModel(
            run_id=run_id,
            experiment_name=payload.name,
            random_seed=payload.seed,
            configuration={
                "policy_herbivore": payload.policy_herbivore,
                "policy_predator": payload.policy_predator,
            },
            status="running",
            metadata_json={"notes": payload.notes} if payload.notes else {},
            started_at=datetime.now(timezone.utc),
        )
        created = self.runs.create(run)
        return self._to_experiment_record(created)

    def list_experiments(self) -> list[ExperimentRecord]:
        runs = self.runs.list_runs(limit=200)
        return [self._to_experiment_record(run) for run in runs]

    def add_stats(self, snapshot: StatsSnapshotIn) -> StatsSnapshotIn:
        if self.runs.get_by_id(snapshot.experimentId) is None:
            placeholder = SimulationRunModel(
                run_id=snapshot.experimentId,
                experiment_name=f"imported-{snapshot.experimentId[:8]}",
                random_seed=None,
                configuration={"source": "v1_stats_import"},
                status="running",
                metadata_json={"auto_created": True},
            )
            self.runs.create(placeholder)

        model = SimulationSnapshotModel(
            run_id=snapshot.experimentId,
            simulation_time=snapshot.simulationTimeSeconds,
            herbivore_population=snapshot.herbivoreCount,
            predator_population=snapshot.predatorCount,
            plant_count=0,
            births=snapshot.births or 0,
            deaths=snapshot.deaths or 0,
            extra_metrics={
                "totalAlive": snapshot.totalAlive,
                "timestampUtcUnix": snapshot.timestampUtcUnix,
                "source": "v1",
                **(
                    {"population_change": snapshot.populationChange}
                    if snapshot.populationChange is not None
                    else {}
                ),
                **({"scripted_alive": snapshot.scriptedAlive} if snapshot.scriptedAlive is not None else {}),
                **({"ppo_alive": snapshot.ppoAlive} if snapshot.ppoAlive is not None else {}),
                **({"max_generation": snapshot.maxGeneration} if snapshot.maxGeneration is not None else {}),
            },
        )
        self.snapshots.add(model)
        return snapshot

    def list_stats(self, experiment_id: str | None = None) -> list[StatsSnapshotIn]:
        snapshots = self.snapshots.list_for_run(experiment_id)
        return [self._to_stats_snapshot(model) for model in snapshots]

    @staticmethod
    def _to_experiment_record(run: SimulationRunModel) -> ExperimentRecord:
        config = run.configuration or {}
        notes = run.metadata_json.get("notes") if run.metadata_json else None
        return ExperimentRecord(
            id=run.run_id,
            name=run.experiment_name,
            policy_herbivore=config.get("policy_herbivore", "scripted_baseline"),
            policy_predator=config.get("policy_predator", "scripted_baseline"),
            seed=run.random_seed or 42,
            notes=notes,
            created_at=run.started_at,
        )

    @staticmethod
    def _to_stats_snapshot(model: SimulationSnapshotModel) -> StatsSnapshotIn:
        metrics = model.extra_metrics or {}
        total_alive = metrics.get("totalAlive")
        if total_alive is None:
            total_alive = model.herbivore_population + model.predator_population
        timestamp = metrics.get("timestampUtcUnix", 0.0)
        return StatsSnapshotIn(
            experimentId=model.run_id,
            simulationTimeSeconds=model.simulation_time,
            herbivoreCount=model.herbivore_population,
            predatorCount=model.predator_population,
            totalAlive=int(total_alive),
            timestampUtcUnix=float(timestamp),
            births=model.births,
            deaths=model.deaths,
            populationChange=_optional_int(metrics.get("population_change")),
            scriptedAlive=_optional_int(metrics.get("scripted_alive")),
            ppoAlive=_optional_int(metrics.get("ppo_alive")),
            maxGeneration=_optional_int(metrics.get("max_generation")),
        )


def _optional_int(value: object) -> int | None:
    if value is None:
        return None
    try:
        return int(value)
    except (TypeError, ValueError):
        return None
