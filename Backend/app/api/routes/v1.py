from fastapi import APIRouter, Depends

from app.api.dependencies import get_v1_service
from app.schemas.v1 import ExperimentCreate, ExperimentRecord, StatsSnapshotIn
from app.services.v1_service import V1Service

router = APIRouter(tags=["v1"])


@router.post("/experiments", response_model=ExperimentRecord)
def create_experiment(payload: ExperimentCreate, service: V1Service = Depends(get_v1_service)) -> ExperimentRecord:
    return service.create_experiment(payload)


@router.get("/experiments", response_model=list[ExperimentRecord])
def list_experiments(service: V1Service = Depends(get_v1_service)) -> list[ExperimentRecord]:
    return service.list_experiments()


@router.post("/stats", response_model=StatsSnapshotIn)
def post_stats(snapshot: StatsSnapshotIn, service: V1Service = Depends(get_v1_service)) -> StatsSnapshotIn:
    return service.add_stats(snapshot)


@router.get("/stats", response_model=list[StatsSnapshotIn])
def get_stats(experiment_id: str | None = None, service: V1Service = Depends(get_v1_service)) -> list[StatsSnapshotIn]:
    return service.list_stats(experiment_id)
