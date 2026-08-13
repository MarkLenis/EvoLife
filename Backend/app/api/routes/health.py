from fastapi import APIRouter, Depends

from app.api.dependencies import get_app_settings
from app.config import Settings
from app.schemas.health import HealthResponse

router = APIRouter(tags=["health"])


@router.get("/health", response_model=HealthResponse)
def health_check(settings: Settings = Depends(get_app_settings)) -> HealthResponse:
    return HealthResponse(status="ok", service=settings.app_name)
