from fastapi import APIRouter, Depends, HTTPException, Query
from pydantic import BaseModel
from sqlalchemy.orm import Session

from ..database import get_db, User
from .auth_routes import client_account_id, get_current_user

router = APIRouter()


@router.get("/account/me")
async def account_me(user: User = Depends(get_current_user)):
    account_id = client_account_id(user)
    display_name = user.display_name or user.username
    profile_image = user.account_photo_url or ""
    created_at = user.created_at.isoformat() if user.created_at else None
    return {
        # Canonical RecNet self-account DTO used by this client.
        "accountId": account_id,
        "username": user.username,
        "displayName": display_name,
        "profileImage": profile_image,
        "isJunior": False,
        "platforms": 0,
        "personalPronouns": 0,
        "identityFlags": 0,
        "createdAt": created_at,
        "email": user.email or None,
        "birthday": None,
        "availableUsernameChanges": 1,
        # Retain legacy aliases for the other local tools using this server.
        "Id": account_id,
        "Username": user.username,
        "DisplayName": display_name,
        "PhotoUrl": profile_image,
        "Bio": user.bio or "",
        "CountryCode": user.country_code or "US",
        "IsDeveloper": user.is_developer,
        "IsRRPlus": user.is_rrplus,
        "PlayerLevel": user.player_level,
        "PlayerXP": user.player_xp,
        "CreatedAt": created_at,
        "LastLogin": user.last_login.isoformat() if user.last_login else None,
        "Email": user.email or "",
    }


@router.get("/account/bulk")
async def account_bulk(id: str = Query(""), ids: str = Query(""), db: Session = Depends(get_db)):
    all_ids = [i.strip() for i in (id or ids).split(",") if i.strip()]
    if not all_ids:
        return []
    users = db.query(User).filter(User.id.in_(all_ids)).all()
    return [
        {
            "accountId": client_account_id(u),
            "username": u.username,
            "displayName": u.display_name or u.username,
            "profileImage": u.account_photo_url or "",
            "isJunior": False,
            "platforms": 0,
            "personalPronouns": 0,
            "identityFlags": 0,
            "createdAt": u.created_at.isoformat() if u.created_at else None,
            "Id": client_account_id(u),
            "Username": u.username,
            "DisplayName": u.display_name or u.username,
            "PhotoUrl": u.account_photo_url or "",
            "Bio": u.bio or "",
            "CountryCode": u.country_code or "US",
            "IsDeveloper": u.is_developer,
            "IsRRPlus": u.is_rrplus,
            "PlayerLevel": u.player_level,
            "PlayerXP": u.player_xp,
            "CreatedAt": u.created_at.isoformat() if u.created_at else None,
        }
        for u in users
    ]


@router.get("/parentalcontrol/me")
async def parental_control_me(user: User = Depends(get_current_user)):
    return {"IsEnabled": False, "Features": None}


class UpdateProfileBody(BaseModel):
    displayName: str | None = None
    bio: str | None = None
    photoUrl: str | None = None


@router.put("/account/me")
async def update_account_me(body: UpdateProfileBody, user: User = Depends(get_current_user), db: Session = Depends(get_db)):
    if body.displayName is not None:
        user.display_name = body.displayName
    if body.bio is not None:
        user.bio = body.bio
    if body.photoUrl is not None:
        user.account_photo_url = body.photoUrl
    db.commit()
    return {"status": "ok"}
