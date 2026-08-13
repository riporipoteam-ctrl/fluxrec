from fastapi import APIRouter, Depends

from ..database import User
from .auth_routes import get_current_user

router = APIRouter()


@router.get("/api/config/v2")
async def config_v2():
    return {
        "Configs": [
            {"Key": "LiveOps.TLA.Recipes.Id", "Value": "00000000-0000-0000-0000-000000000000"},
            {"Key": "LiveOps.TLA.IsEnabled", "Value": "false"},
            # This client reports v=20230302 on its version check; the old value
            # here belonged to the newer Unity 6 build, not this depot.
            {"Key": "RecNet.GameVersion", "Value": "20230302"},
            {"Key": "RecNet.Environment", "Value": "prod"},
        ]
    }


@router.get("/api/config/v1/amplitude")
async def amplitude_config():
    """Analytics config, polled during boot.

    This previously fell through to the catch-all, which answers 200 with
    {"error": "unimplemented", ...}. That is a valid HTTP response but not a
    shape the client can read, so return a real disabled-analytics config.
    """
    return {
        "ApiKey": "",
        "Enabled": False,
        "IsEnabled": False,
        "ServerUrl": "",
        "SampleRate": 0,
        "FlushIntervalMillis": 60000,
        "FlushQueueSize": 30,
    }


@router.post("/api/gamesight/event")
@router.post("/api/gamesight/events")
async def gamesight_event():
    return {"Success": True, "success": True}


@router.post("/v1/events")
@router.post("/data/event")
@router.post("/data/heartbeat")
async def analytics_sink():
    return {"Success": True}


@router.get("/api/PlayerReporting/v1/voteToKickReasons")
async def vote_to_kick_reasons():
    return {
        "Reasons": [
            {"Id": 1, "Name": "Harassment"},
            {"Id": 2, "Name": "Cheating"},
            {"Id": 3, "Name": "Spam"},
            {"Id": 4, "Name": "Inappropriate"},
        ]
    }


@router.get("/api/PlayerReporting/v1/moderationBlockDetails")
async def moderation_block_details(user: User = Depends(get_current_user)):
    return {"IsBlocked": False, "BlockReason": None, "BlockExpiry": None}


@router.post("/api/PlayerReporting/v1/roomModKick")
async def room_mod_kick():
    return {"status": "ok"}


@router.get("/api/images/v2/named")
async def named_images():
    return []


@router.get("/api/images/v5/cheered/bulk")
async def cheered_images_bulk():
    return []


@router.post("/api/images/v1/cheer")
async def cheer_image():
    return {"status": "ok"}


@router.post("/api/PlayerCheer/v1/create")
async def create_player_cheer():
    return {"status": "ok"}


@router.get("/statsigUserProperties")
async def statsig_user_properties(user: User = Depends(get_current_user)):
    return {
        "userID": user.id,
        "properties": {
            "platform": "steam",
            "gameVersion": "20260323.14",
            "countryCode": user.country_code or "US",
        },
    }


@router.post("/statsigUserProperties")
async def update_statsig_properties():
    return {"status": "ok"}


@router.get("/api/players/v1/playerPhotoTaggingSetting")
async def player_photo_tagging():
    return {"PlayerPhotoTaggingSetting": 0}


@router.get("/api/players/v2/progression/bulk")
async def players_progression_bulk():
    return []
