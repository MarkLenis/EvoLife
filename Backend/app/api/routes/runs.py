from fastapi import APIRouter, Depends, HTTPException, Query

from app.api.dependencies import get_analytics_service
from app.persistence.repositories import RunNotFoundError
from app.schemas.run import (
    SimulationRunCreate,
    SimulationRunDetailResponse,
    SimulationRunFinish,
    SimulationRunListResponse,
    SimulationRunResponse,
)
from app.services.analytics_service import AnalyticsService

router = APIRouter(prefix="/runs", tags=["runs"])


def _not_found(exc: RunNotFoundError) -> HTTPException:
    return HTTPException(status_code=404, detail=str(exc))


@router.post("", response_model=SimulationRunResponse, status_code=201)
def create_simulation_run(
    payload: SimulationRunCreate,
    service: AnalyticsService = Depends(get_analytics_service),
) -> SimulationRunResponse:
    return service.create_run(payload)


@router.post("/{run_id}/finish", response_model=SimulationRunResponse)
def finish_simulation_run(
    run_id: str,
    payload: SimulationRunFinish,
    service: AnalyticsService = Depends(get_analytics_service),
) -> SimulationRunResponse:
    try:
        return service.finish_run(run_id, payload)
    except RunNotFoundError as exc:
        raise _not_found(exc) from exc


@router.get("/{run_id}", response_model=SimulationRunDetailResponse)
def get_simulation_run(
    run_id: str,
    service: AnalyticsService = Depends(get_analytics_service),
) -> SimulationRunDetailResponse:
    try:
        return service.get_run(run_id)
    except RunNotFoundError as exc:
        raise _not_found(exc) from exc


@router.get("", response_model=SimulationRunListResponse)
def list_simulation_runs(
    experiment_name: str | None = Query(default=None),
    status: str | None = Query(default=None),
    limit: int = Query(default=50, ge=1, le=200),
    offset: int = Query(default=0, ge=0),
    service: AnalyticsService = Depends(get_analytics_service),
) -> SimulationRunListResponse:
    return service.list_runs(
        experiment_name=experiment_name,
        status=status,
        limit=limit,
        offset=offset,
    )
