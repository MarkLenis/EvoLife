from fastapi import APIRouter, Depends

from app.api.dependencies import get_analytics_service
from app.api.routes.runs import _not_found
from app.persistence.repositories import RunNotFoundError
from app.schemas.generation import GenerationSummaryBatchCreate, GenerationSummaryBatchResponse
from app.services.analytics_service import AnalyticsService

router = APIRouter(prefix="/runs/{run_id}/generations", tags=["generations"])


@router.post("", response_model=GenerationSummaryBatchResponse, status_code=201)
def submit_generation_summaries(
    run_id: str,
    payload: GenerationSummaryBatchCreate,
    service: AnalyticsService = Depends(get_analytics_service),
) -> GenerationSummaryBatchResponse:
    try:
        summaries = service.add_generation_summaries(run_id, payload.summaries)
    except RunNotFoundError as exc:
        raise _not_found(exc) from exc
    return GenerationSummaryBatchResponse(inserted=len(summaries), summaries=summaries)
