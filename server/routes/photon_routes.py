import uuid
from fastapi import APIRouter, Depends

from ..database import User
from ..config import PHOTON_APP_ID, PHOTON_REGION
from .auth_routes import get_current_user

router = APIRouter()


@router.get("/photon/app")
async def photon_app():
    return {
        "AppId": PHOTON_APP_ID,
        "AppVersion": "1.0.0.0",
        "Region": PHOTON_REGION,
        "Protocol": "tcp",
        "UseDefault": True,
    }


@router.get("/photon/token")
async def photon_token(user: User = Depends(get_current_user)):
    return {
        "UserId": user.id,
        "Username": user.username,
        "DisplayName": user.display_name or user.username,
        "Token": str(uuid.uuid4()),
        "AppId": PHOTON_APP_ID,
    }


@router.get("/photon/regions")
async def photon_regions():
    return {
        "Regions": [
            {"Name": "us", "Address": "127.0.0.1", "Port": 5055, "IsBest": True},
            {"Name": "eu", "Address": "127.0.0.1", "Port": 5055, "IsBest": False},
            {"Name": "asia", "Address": "127.0.0.1", "Port": 5055, "IsBest": False},
        ],
        "BestRegion": "us",
    }
