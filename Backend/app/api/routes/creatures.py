from fastapi import APIRouter, Depends

from app.api.dependencies import get_analytics_service
from app.api.routes.runs import _not_found
from app.persistence.repositories import RunNotFoundError
from app.schemas.creature import CreatureLifeRecordBatchCreate, CreatureLifeRecordBatchResponse
from app.services.analytics_service import AnalyticsService

router = APIRouter(prefix="/runs/{run_id}/creatures", tags=["creatures"])


@router.post("", response_model=CreatureLifeRecordBatchResponse, status_code=201)
def submit_creature_records(
    run_id: str,
    payload: CreatureLifeRecordBatchCreate,
    service: AnalyticsService = Depends(get_analytics_service),
) -> CreatureLifeRecordBatchResponse:
    try:
        records = service.add_creature_records(run_id, payload.records)
    except RunNotFoundError as exc:
        raise _not_found(exc) from exc
    return CreatureLifeRecordBatchResponse(inserted=len(records), records=records)
