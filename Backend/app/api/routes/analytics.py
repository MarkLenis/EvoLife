from fastapi import APIRouter, Depends

from app.api.dependencies import get_analytics_service
from app.api.routes.runs import _not_found
from app.persistence.repositories import RunNotFoundError
from app.schemas.generation import EvolutionTimeSeriesResponse
from app.schemas.snapshot import PopulationTimeSeriesResponse
from app.services.analytics_service import AnalyticsService

router = APIRouter(prefix="/runs/{run_id}", tags=["analytics"])


@router.get("/population-series", response_model=PopulationTimeSeriesResponse)
def get_population_time_series(
    run_id: str,
    service: AnalyticsService = Depends(get_analytics_service),
) -> PopulationTimeSeriesResponse:
    try:
        return service.get_population_time_series(run_id)
    except RunNotFoundError as exc:
        raise _not_found(exc) from exc


@router.get("/evolution-series", response_model=EvolutionTimeSeriesResponse)
def get_evolution_time_series(
    run_id: str,
    species: str | None = None,
    service: AnalyticsService = Depends(get_analytics_service),
) -> EvolutionTimeSeriesResponse:
    try:
        return service.get_evolution_time_series(run_id, species=species)
    except RunNotFoundError as exc:
        raise _not_found(exc) from exc
