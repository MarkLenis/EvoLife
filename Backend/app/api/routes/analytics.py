from fastapi import APIRouter, Depends, Query

from app.api.dependencies import get_analytics_service
from app.api.routes.runs import _not_found
from app.persistence.repositories import RunNotFoundError
from app.schemas.evaluation import PolicyComparisonResponse, SurvivalRecordsResponse, TraitEvolutionResponse
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


@router.get("/policy-comparison", response_model=PolicyComparisonResponse)
def get_policy_comparison(
    run_id: str,
    service: AnalyticsService = Depends(get_analytics_service),
) -> PolicyComparisonResponse:
    try:
        return service.get_policy_comparison(run_id)
    except RunNotFoundError as exc:
        raise _not_found(exc) from exc


@router.get("/survival", response_model=SurvivalRecordsResponse)
def get_survival_records(
    run_id: str,
    policy_kind: str | None = Query(default=None),
    species: str | None = Query(default=None),
    service: AnalyticsService = Depends(get_analytics_service),
) -> SurvivalRecordsResponse:
    try:
        return service.get_survival_records(run_id, policy_kind=policy_kind, species=species)
    except RunNotFoundError as exc:
        raise _not_found(exc) from exc


@router.get("/trait-evolution", response_model=TraitEvolutionResponse)
def get_trait_evolution(
    run_id: str,
    trait: str = Query(..., min_length=1),
    species: str | None = Query(default=None),
    policy_kind: str | None = Query(default=None),
    service: AnalyticsService = Depends(get_analytics_service),
) -> TraitEvolutionResponse:
    try:
        return service.get_trait_evolution(run_id, trait, species=species, policy_kind=policy_kind)
    except RunNotFoundError as exc:
        raise _not_found(exc) from exc
