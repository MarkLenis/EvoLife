import uuid
from datetime import datetime, timezone

from sqlalchemy import (
    DateTime,
    Float,
    ForeignKey,
    Integer,
    JSON,
    String,
    Text,
    UniqueConstraint,
)
from sqlalchemy.orm import DeclarativeBase, Mapped, mapped_column, relationship


class Base(DeclarativeBase):
    pass


class SimulationRunModel(Base):
    __tablename__ = "simulation_runs"

    run_id: Mapped[str] = mapped_column(String(36), primary_key=True, default=lambda: str(uuid.uuid4()))
    started_at: Mapped[datetime] = mapped_column(
        DateTime(timezone=True),
        default=lambda: datetime.now(timezone.utc),
        nullable=False,
    )
    finished_at: Mapped[datetime | None] = mapped_column(DateTime(timezone=True), nullable=True)
    experiment_name: Mapped[str] = mapped_column(String(255), nullable=False, index=True)
    random_seed: Mapped[int | None] = mapped_column(Integer, nullable=True)
    configuration: Mapped[dict] = mapped_column(JSON, default=dict, nullable=False)
    status: Mapped[str] = mapped_column(String(32), default="running", nullable=False, index=True)
    metadata_json: Mapped[dict] = mapped_column("metadata", JSON, default=dict, nullable=False)

    snapshots: Mapped[list["SimulationSnapshotModel"]] = relationship(
        back_populates="run",
        cascade="all, delete-orphan",
        order_by="SimulationSnapshotModel.simulation_time",
    )
    creatures: Mapped[list["CreatureLifeRecordModel"]] = relationship(
        back_populates="run",
        cascade="all, delete-orphan",
    )
    generations: Mapped[list["GenerationSummaryModel"]] = relationship(
        back_populates="run",
        cascade="all, delete-orphan",
        order_by="GenerationSummaryModel.generation",
    )


class SimulationSnapshotModel(Base):
    __tablename__ = "simulation_snapshots"
    __table_args__ = (UniqueConstraint("run_id", "simulation_time", name="uq_snapshot_run_time"),)

    id: Mapped[int] = mapped_column(Integer, primary_key=True, autoincrement=True)
    run_id: Mapped[str] = mapped_column(ForeignKey("simulation_runs.run_id", ondelete="CASCADE"), index=True)
    simulation_time: Mapped[float] = mapped_column(Float, nullable=False)
    herbivore_population: Mapped[int] = mapped_column(Integer, nullable=False)
    predator_population: Mapped[int] = mapped_column(Integer, nullable=False)
    plant_count: Mapped[int] = mapped_column(Integer, default=0, nullable=False)
    births: Mapped[int] = mapped_column(Integer, default=0, nullable=False)
    deaths: Mapped[int] = mapped_column(Integer, default=0, nullable=False)
    average_herbivore_speed: Mapped[float | None] = mapped_column(Float, nullable=True)
    average_predator_speed: Mapped[float | None] = mapped_column(Float, nullable=True)
    average_vision: Mapped[float | None] = mapped_column(Float, nullable=True)
    average_lifespan: Mapped[float | None] = mapped_column(Float, nullable=True)
    average_energy: Mapped[float | None] = mapped_column(Float, nullable=True)
    extra_metrics: Mapped[dict] = mapped_column(JSON, default=dict, nullable=False)

    run: Mapped["SimulationRunModel"] = relationship(back_populates="snapshots")


class CreatureLifeRecordModel(Base):
    __tablename__ = "creature_life_records"

    id: Mapped[int] = mapped_column(Integer, primary_key=True, autoincrement=True)
    run_id: Mapped[str] = mapped_column(ForeignKey("simulation_runs.run_id", ondelete="CASCADE"), index=True)
    creature_id: Mapped[str] = mapped_column(String(64), nullable=False, index=True)
    species: Mapped[str] = mapped_column(String(64), nullable=False, index=True)
    generation: Mapped[int] = mapped_column(Integer, nullable=False, index=True)
    birth_time: Mapped[float] = mapped_column(Float, nullable=False)
    death_time: Mapped[float | None] = mapped_column(Float, nullable=True)
    cause_of_death: Mapped[str | None] = mapped_column(String(128), nullable=True)
    parent_id_1: Mapped[str | None] = mapped_column(String(64), nullable=True)
    parent_id_2: Mapped[str | None] = mapped_column(String(64), nullable=True)
    offspring_count: Mapped[int] = mapped_column(Integer, default=0, nullable=False)
    genome_traits: Mapped[dict] = mapped_column(JSON, default=dict, nullable=False)
    policy_kind: Mapped[str | None] = mapped_column(String(64), nullable=True, index=True)
    extra_fields: Mapped[dict] = mapped_column(JSON, default=dict, nullable=False)

    run: Mapped["SimulationRunModel"] = relationship(back_populates="creatures")


class GenerationSummaryModel(Base):
    __tablename__ = "generation_summaries"
    __table_args__ = (
        UniqueConstraint("run_id", "species", "generation", name="uq_generation_run_species_gen"),
    )

    id: Mapped[int] = mapped_column(Integer, primary_key=True, autoincrement=True)
    run_id: Mapped[str] = mapped_column(ForeignKey("simulation_runs.run_id", ondelete="CASCADE"), index=True)
    species: Mapped[str] = mapped_column(String(64), nullable=False, index=True)
    generation: Mapped[int] = mapped_column(Integer, nullable=False, index=True)
    population_count: Mapped[int] = mapped_column(Integer, nullable=False)
    average_genome_traits: Mapped[dict] = mapped_column(JSON, default=dict, nullable=False)
    average_lifespan: Mapped[float | None] = mapped_column(Float, nullable=True)
    reproduction_rate: Mapped[float | None] = mapped_column(Float, nullable=True)
    offspring_per_parent: Mapped[float | None] = mapped_column(Float, nullable=True)
    notes: Mapped[str | None] = mapped_column(Text, nullable=True)
    extra_statistics: Mapped[dict] = mapped_column(JSON, default=dict, nullable=False)

    run: Mapped["SimulationRunModel"] = relationship(back_populates="generations")
