from fastapi import APIRouter, Depends

from app.api.dependencies import get_analytics_service
from app.api.routes.runs import _not_found
from app.persistence.repositories import RunNotFoundError
from app.schemas.snapshot import (
    PopulationTimeSeriesResponse,
    SimulationSnapshotBatchCreate,
    SimulationSnapshotBatchResponse,
    SimulationSnapshotCreate,
    SimulationSnapshotResponse,
)
from app.services.analytics_service import AnalyticsService

router = APIRouter(prefix="/runs/{run_id}/snapshots", tags=["snapshots"])


@router.post("", response_model=SimulationSnapshotResponse, status_code=201)
def submit_snapshot(
    run_id: str,
    payload: SimulationSnapshotCreate,
    service: AnalyticsService = Depends(get_analytics_service),
) -> SimulationSnapshotResponse:
    try:
        return service.add_snapshot(run_id, payload)
    except RunNotFoundError as exc:
        raise _not_found(exc) from exc


@router.post("/batch", response_model=SimulationSnapshotBatchResponse, status_code=201)
def submit_snapshots_batch(
    run_id: str,
    payload: SimulationSnapshotBatchCreate,
    service: AnalyticsService = Depends(get_analytics_service),
) -> SimulationSnapshotBatchResponse:
    try:
        snapshots = service.add_snapshots_batch(run_id, payload.snapshots)
    except RunNotFoundError as exc:
        raise _not_found(exc) from exc
    return SimulationSnapshotBatchResponse(inserted=len(snapshots), snapshots=snapshots)
