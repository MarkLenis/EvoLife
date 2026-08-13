from datetime import datetime, timezone

from sqlalchemy import select
from sqlalchemy.orm import Session, selectinload

from app.persistence.models import (
    CreatureLifeRecordModel,
    GenerationSummaryModel,
    SimulationRunModel,
    SimulationSnapshotModel,
)


class RunNotFoundError(Exception):
    def __init__(self, run_id: str) -> None:
        self.run_id = run_id
        super().__init__(f"Simulation run '{run_id}' was not found.")


class RunRepository:
    def __init__(self, session: Session) -> None:
        self.session = session

    def create(self, run: SimulationRunModel) -> SimulationRunModel:
        self.session.add(run)
        self.session.commit()
        self.session.refresh(run)
        return run

    def get_by_id(self, run_id: str) -> SimulationRunModel | None:
        return self.session.get(SimulationRunModel, run_id)

    def get_by_id_or_raise(self, run_id: str) -> SimulationRunModel:
        run = self.get_by_id(run_id)
        if run is None:
            raise RunNotFoundError(run_id)
        return run

    def get_with_details(self, run_id: str) -> SimulationRunModel | None:
        stmt = (
            select(SimulationRunModel)
            .where(SimulationRunModel.run_id == run_id)
            .options(
                selectinload(SimulationRunModel.snapshots),
                selectinload(SimulationRunModel.creatures),
                selectinload(SimulationRunModel.generations),
            )
        )
        return self.session.scalar(stmt)

    def list_runs(
        self,
        *,
        experiment_name: str | None = None,
        status: str | None = None,
        limit: int = 50,
        offset: int = 0,
    ) -> list[SimulationRunModel]:
        stmt = select(SimulationRunModel).order_by(SimulationRunModel.started_at.desc())
        if experiment_name is not None:
            stmt = stmt.where(SimulationRunModel.experiment_name == experiment_name)
        if status is not None:
            stmt = stmt.where(SimulationRunModel.status == status)
        stmt = stmt.limit(limit).offset(offset)
        return list(self.session.scalars(stmt).all())

    def finish(self, run_id: str, *, status: str, finished_at: datetime | None = None) -> SimulationRunModel:
        run = self.get_by_id_or_raise(run_id)
        run.status = status
        run.finished_at = finished_at or datetime.now(timezone.utc)
        self.session.commit()
        self.session.refresh(run)
        return run


class SnapshotRepository:
    def __init__(self, session: Session) -> None:
        self.session = session

    def add(self, snapshot: SimulationSnapshotModel) -> SimulationSnapshotModel:
        self.session.add(snapshot)
        self.session.commit()
        self.session.refresh(snapshot)
        return snapshot

    def add_many(self, snapshots: list[SimulationSnapshotModel]) -> list[SimulationSnapshotModel]:
        self.session.add_all(snapshots)
        self.session.commit()
        for snapshot in snapshots:
            self.session.refresh(snapshot)
        return snapshots

    def list_for_run(self, run_id: str) -> list[SimulationSnapshotModel]:
        stmt = (
            select(SimulationSnapshotModel)
            .where(SimulationSnapshotModel.run_id == run_id)
            .order_by(SimulationSnapshotModel.simulation_time.asc())
        )
        return list(self.session.scalars(stmt).all())


class CreatureRepository:
    def __init__(self, session: Session) -> None:
        self.session = session

    def add_many(self, records: list[CreatureLifeRecordModel]) -> list[CreatureLifeRecordModel]:
        self.session.add_all(records)
        self.session.commit()
        for record in records:
            self.session.refresh(record)
        return records

    def list_for_run(self, run_id: str, species: str | None = None) -> list[CreatureLifeRecordModel]:
        stmt = select(CreatureLifeRecordModel).where(CreatureLifeRecordModel.run_id == run_id)
        if species is not None:
            stmt = stmt.where(CreatureLifeRecordModel.species == species)
        stmt = stmt.order_by(CreatureLifeRecordModel.birth_time.asc())
        return list(self.session.scalars(stmt).all())


class GenerationRepository:
    def __init__(self, session: Session) -> None:
        self.session = session

    def add_many(self, summaries: list[GenerationSummaryModel]) -> list[GenerationSummaryModel]:
        self.session.add_all(summaries)
        self.session.commit()
        for summary in summaries:
            self.session.refresh(summary)
        return summaries

    def list_for_run(self, run_id: str, species: str | None = None) -> list[GenerationSummaryModel]:
        stmt = select(GenerationSummaryModel).where(GenerationSummaryModel.run_id == run_id)
        if species is not None:
            stmt = stmt.where(GenerationSummaryModel.species == species)
        stmt = stmt.order_by(GenerationSummaryModel.generation.asc(), GenerationSummaryModel.species.asc())
        return list(self.session.scalars(stmt).all())
