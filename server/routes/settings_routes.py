import json
from typing import Any

from fastapi import APIRouter, Depends
from sqlalchemy.orm import Session

from ..database import get_db, User, PlayerSettings
from .auth_routes import get_current_user

router = APIRouter()


def load_settings(record: PlayerSettings | None) -> dict[str, Any]:
    if not record or not record.settings_json:
        return {}
    try:
        value = json.loads(record.settings_json)
        return value if isinstance(value, dict) else {}
    except (TypeError, ValueError):
        return {}


def recnet_value(value: Any) -> str:
    """The 2023 client stores every cloud preference as a string."""
    if isinstance(value, bool):
        return "true" if value else "false"
    if value is None:
        return ""
    if isinstance(value, (dict, list)):
        return json.dumps(value, separators=(",", ":"))
    return str(value)


def merge_settings(current: dict[str, Any], body: dict[str, Any]) -> dict[str, Any]:
    # The legacy RecNet client saves one preference at a time as
    # {"Key": "...", "Value": "..."}. Keep accepting the newer wrapper and
    # plain dictionaries as well so Flux and local diagnostics share the store.
    key = body.get("Key", body.get("key"))
    if key is not None:
        value = body.get("Value", body.get("value", ""))
        current[str(key)] = recnet_value(value)
        return current

    wrapped = body.get("Settings", body.get("settings"))
    source = wrapped if isinstance(wrapped, dict) else body
    for setting_key, setting_value in source.items():
        if setting_key not in {"UserId", "userId", "Version", "version"}:
            current[str(setting_key)] = recnet_value(setting_value)
    return current


@router.get("/playersettings")
@router.get("/api/playersettings/v1")
@router.get("/api/player/settings")
async def get_player_settings(
    user: User = Depends(get_current_user),
    db: Session = Depends(get_db),
):
    record = db.query(PlayerSettings).filter(PlayerSettings.user_id == user.id).first()
    settings = load_settings(record)

    # NBDIDJMANNH.HBIOOAPLHOL in this depot deserializes the endpoint directly
    # as List<OJFCCBFDEMA>; each item is the public RecNet Key/Value shape.
    return [
        {"Key": key, "Value": recnet_value(value)}
        for key, value in sorted(settings.items())
    ]


@router.put("/playersettings")
@router.put("/api/playersettings/v1")
@router.put("/api/player/settings")
@router.post("/playersettings")
@router.post("/api/playersettings/v1")
@router.post("/api/player/settings")
async def update_player_settings(
    body: dict,
    user: User = Depends(get_current_user),
    db: Session = Depends(get_db),
):
    record = db.query(PlayerSettings).filter(PlayerSettings.user_id == user.id).first()
    current = merge_settings(load_settings(record), body)
    if not record:
        record = PlayerSettings(
            user_id=user.id,
            settings_json=json.dumps(current, separators=(",", ":")),
        )
        db.add(record)
    else:
        record.settings_json = json.dumps(current, separators=(",", ":"))
    db.commit()
    return {"Success": True, "Saved": True}
