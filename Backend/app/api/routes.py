from fastapi import APIRouter

from app.models.schemas import ExperimentCreate, ExperimentRecord, StatsSnapshotIn
from app.services.store import store

router = APIRouter()


@router.post("/experiments", response_model=ExperimentRecord)
def create_experiment(payload: ExperimentCreate) -> ExperimentRecord:
    return store.create_experiment(payload)


@router.get("/experiments", response_model=list[ExperimentRecord])
def list_experiments() -> list[ExperimentRecord]:
    return store.list_experiments()


@router.post("/stats", response_model=StatsSnapshotIn)
def post_stats(snapshot: StatsSnapshotIn) -> StatsSnapshotIn:
    return store.add_stats(snapshot)


@router.get("/stats", response_model=list[StatsSnapshotIn])
def get_stats(experiment_id: str | None = None) -> list[StatsSnapshotIn]:
    return store.list_stats(experiment_id)
