from __future__ import annotations

from threading import Lock
from typing import Dict, List
from uuid import uuid4

from app.models.schemas import ExperimentCreate, ExperimentRecord, StatsSnapshotIn


class InMemoryStore:
    """Process-local store for early development. Replace with a DB later."""

    def __init__(self) -> None:
        self._lock = Lock()
        self._experiments: Dict[str, ExperimentRecord] = {}
        self._stats: List[StatsSnapshotIn] = []

    def create_experiment(self, payload: ExperimentCreate) -> ExperimentRecord:
        record = ExperimentRecord(
            id=str(uuid4()),
            name=payload.name,
            policy_herbivore=payload.policy_herbivore,
            policy_predator=payload.policy_predator,
            seed=payload.seed,
            notes=payload.notes,
        )
        with self._lock:
            self._experiments[record.id] = record
        return record

    def list_experiments(self) -> List[ExperimentRecord]:
        with self._lock:
            return list(self._experiments.values())

    def add_stats(self, snapshot: StatsSnapshotIn) -> StatsSnapshotIn:
        with self._lock:
            self._stats.append(snapshot)
        return snapshot

    def list_stats(self, experiment_id: str | None = None) -> List[StatsSnapshotIn]:
        with self._lock:
            if experiment_id is None:
                return list(self._stats)
            return [s for s in self._stats if s.experimentId == experiment_id]


store = InMemoryStore()
