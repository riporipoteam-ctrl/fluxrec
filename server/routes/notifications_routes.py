import uuid
from fastapi import APIRouter, Depends

from ..database import User
from .auth_routes import get_current_user

router = APIRouter()


@router.post("/hub/v1/negotiate")
async def negotiate_hub(user: User = Depends(get_current_user)):
    return {
        "url": "http://127.0.0.1:8081/notifications",
        "accessToken": str(uuid.uuid4()),
        "connectionId": str(uuid.uuid4()),
        "userId": user.id,
    }


@router.get("/hub/v1")
async def hub_v1():
    return {"HubEnabled": True, "HubUrl": ""}


@router.get("/crm/me/config/v3")
async def crm_config(user: User = Depends(get_current_user)):
    return {
        "IsEnabled": True,
        "ChatNotifications": True,
        "FriendNotifications": True,
        "RoomNotifications": True,
        "ClubNotifications": True,
    }
