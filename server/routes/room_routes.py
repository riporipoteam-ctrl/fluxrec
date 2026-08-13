import json
import uuid
from datetime import datetime, timezone

from fastapi import APIRouter, Depends, HTTPException, Query
from pydantic import BaseModel
from sqlalchemy.orm import Session
from sqlalchemy import desc

from ..database import get_db, User, Room, RoomSave, RoomVisit, Outfit
from .auth_routes import get_current_user, get_optional_user

router = APIRouter()


def orientation_room_dict() -> dict:
    # This depot's stock OAILMIHJFAK preset identifies the bundled offline
    # Orientation room as RoomId -2 and LKHDFEKNHMI.ORIENTATION (14).  The
    # client resolves this record before it can start the real loading-screen
    # coroutine, even though the scene itself is already in the game files.
    return {
        "RoomId": -2,
        "Name": "Orientation",
        "Description": "Welcome to Rec Room",
        "ImageName": "Orientation",
        "ImageUrl": "",
        "OwnerId": 0,
        "IsPrivate": True,
        "IsFeatured": False,
        "IsRRO": True,
        "IsDorm": False,
        "Capacity": 1,
        "PlayerCount": 0,
        "RoomVersion": 1,
        "SubroomCount": 0,
        "SubRooms": [],
        "Tags": [],
        "PlatformMask": -1,
        "RoomType": 14,
        "IsRoomApproved": True,
        "SupportsTeleporter": True,
        "SupportsRoomDoor": False,
        "CreatedAt": None,
        "UpdatedAt": None,
    }


def new_legacy_room_id() -> str:
    # The 2022 IL2CPP client deserializes RoomId as Int64. Keep UUIDs for
    # account/internal identifiers, but generate numeric room ids for rooms
    # that this legacy client must deserialize.
    return str((uuid.uuid4().int % 8_000_000_000_000_000_000) + 1)


def response_room_id(room_id: str):
    value = str(room_id)
    return int(value) if value.lstrip("-").isdigit() else value


def room_to_dict(room: Room) -> dict:
    return {
        "RoomId": response_room_id(room.room_id),
        "Name": room.name,
        "Description": room.description,
        "OwnerId": room.owner_id,
        "IsPrivate": room.is_private,
        "IsFeatured": room.is_featured,
        "Capacity": room.capacity,
        "PlayerCount": room.player_count,
        "RoomVersion": room.room_version,
        "SubroomCount": room.subroom_count,
        "Tags": json.loads(room.tags) if room.tags else [],
        "ImageUrl": room.image_url or "",
        "PlatformMask": room.platform_mask,
        "RoomType": room.room_type,
        "IsRoomApproved": room.is_room_approved,
        "SupportsTeleporter": room.supports_teleporter,
        "SupportsRoomDoor": room.supports_room_door,
        "CreatedAt": room.created_at.isoformat() if room.created_at else None,
        "UpdatedAt": room.updated_at.isoformat() if room.updated_at else None,
    }


@router.get("/rooms")
async def list_rooms(
    name: str | None = Query(None),
    skip: int = Query(0, alias="skip"),
    take: int = Query(50, alias="take"),
    db: Session = Depends(get_db),
    user: User | None = Depends(get_optional_user),
):
    if name and name.casefold() == "orientation":
        return [orientation_room_dict()]

    query = db.query(Room)
    if name:
        query = query.filter(Room.name.ilike(f"%{name}%"))
    rooms = query.offset(skip).limit(take).all()
    return [room_to_dict(r) for r in rooms]


class CreateRoomBody(BaseModel):
    name: str
    description: str = ""
    isPrivate: bool = False
    roomType: str = "private"


@router.post("/rooms")
async def create_room(
    body: CreateRoomBody,
    user: User = Depends(get_current_user),
    db: Session = Depends(get_db),
):
    room_id = str(uuid.uuid4())
    room = Room(
        room_id=room_id,
        name=body.name,
        description=body.description,
        owner_id=user.id,
        is_private=body.isPrivate,
        room_type=body.roomType,
    )
    db.add(room)
    db.commit()
    db.refresh(room)
    return room_to_dict(room)


@router.get("/rooms/bulk")
async def bulk_rooms(id: str = Query(""), ids: str = Query(""), db: Session = Depends(get_db)):
    all_ids = [i.strip() for i in (id or ids).split(",") if i.strip()]
    if not all_ids:
        return []
    include_orientation = "-2" in all_ids
    all_ids = [room_id for room_id in all_ids if room_id != "-2"]
    rooms = db.query(Room).filter(Room.room_id.in_(all_ids)).all()
    result = [room_to_dict(r) for r in rooms]
    if include_orientation:
        result.insert(0, orientation_room_dict())
    return result


@router.get("/rooms/hot")
async def hot_rooms(db: Session = Depends(get_db)):
    rooms = db.query(Room).order_by(desc(Room.player_count)).limit(20).all()
    return [room_to_dict(r) for r in rooms]


@router.get("/rooms/search")
async def search_rooms(
    q: str = Query("", alias="query"),
    skip: int = Query(0),
    take: int = Query(50),
    db: Session = Depends(get_db),
):
    rooms = db.query(Room).filter(Room.name.ilike(f"%{q}%")).offset(skip).limit(take).all()
    return [room_to_dict(r) for r in rooms]


@router.get("/rooms/autocomplete_search")
async def autocomplete_search(query: str = Query(""), db: Session = Depends(get_db)):
    rooms = db.query(Room).filter(Room.name.ilike(f"%{query}%")).limit(10).all()
    return [r.name for r in rooms]


@router.get("/rooms/ownedby/me")
async def rooms_owned_by_me(
    user: User = Depends(get_current_user),
    db: Session = Depends(get_db),
):
    rooms = db.query(Room).filter(Room.owner_id == user.id).all()
    return [room_to_dict(r) for r in rooms]


@router.get("/rooms/visitedby/me")
async def rooms_visited_by_me(
    user: User = Depends(get_current_user),
    db: Session = Depends(get_db),
):
    visits = db.query(RoomVisit).filter(RoomVisit.user_id == user.id).order_by(desc(RoomVisit.visited_at)).limit(50).all()
    room_ids = [v.room_id for v in visits]
    rooms = db.query(Room).filter(Room.room_id.in_(room_ids)).all()
    room_map = {r.room_id: r for r in rooms}
    result = []
    for v in visits:
        r = room_map.get(v.room_id)
        if r:
            result.append(room_to_dict(r))
    return result


@router.get("/rooms/{room_id}")
async def get_room(room_id: str, db: Session = Depends(get_db)):
    if room_id == "-2":
        return orientation_room_dict()

    room = db.query(Room).filter(Room.room_id == room_id).first()
    if not room:
        raise HTTPException(status_code=404, detail="Room not found")
    return room_to_dict(room)


@router.get("/dormroom/me")
async def dormroom_me(
    user: User = Depends(get_current_user),
    db: Session = Depends(get_db),
):
    rooms = db.query(Room).filter(
        Room.owner_id == user.id,
        Room.room_type == "dorm",
    ).all()
    room = next(
        (
            candidate
            for candidate in rooms
            if str(candidate.room_id).lstrip("-").isdigit()
        ),
        None,
    )
    if not room:
        room_id = new_legacy_room_id()
        room = Room(
            room_id=room_id,
            name=f"{user.display_name or user.username}'s Dorm",
            description="Your dorm room",
            owner_id=user.id,
            room_type="dorm",
            is_private=True,
        )
        db.add(room)
        db.commit()
        db.refresh(room)
    return room_to_dict(room)


@router.get("/featuredrooms/current")
async def featured_rooms(db: Session = Depends(get_db)):
    rooms = db.query(Room).filter(Room.is_featured == True).limit(20).all()
    return [room_to_dict(r) for r in rooms]


@router.get("/rooms/{room_id}/experience")
async def room_experience(room_id: str, db: Session = Depends(get_db)):
    if room_id == "-2":
        room = orientation_room_dict()
        return {
            "RoomId": room["RoomId"],
            "Name": room["Name"],
            "Description": room["Description"],
            "RoomVersion": room["RoomVersion"],
            "SubroomCount": room["SubroomCount"],
        }

    room = db.query(Room).filter(Room.room_id == room_id).first()
    if not room:
        raise HTTPException(status_code=404, detail="Room not found")
    return {
        "RoomId": room.room_id,
        "Name": room.name,
        "Description": room.description,
        "RoomVersion": room.room_version,
        "SubroomCount": room.subroom_count,
    }


@router.get("/rooms/{room_id}/experience/player")
async def room_experience_player(room_id: str, user: User = Depends(get_current_user)):
    return {"RoomId": room_id, "PlayerData": {"LastRoomVisit": None}}


@router.get("/rooms/{room_id}/interactionby/me")
async def room_interaction(room_id: str, user: User = Depends(get_current_user)):
    return {"RoomId": room_id, "IsFavorite": False, "IsIgnored": False, "IsSubscribed": False}


@router.get("/rooms/{room_id}/subrooms/{subroom_index}/saves/{save_index}")
async def get_subroom_save(
    room_id: str,
    subroom_index: int,
    save_index: int,
    db: Session = Depends(get_db),
):
    save = db.query(RoomSave).filter(
        RoomSave.room_id == room_id,
        RoomSave.subroom_index == subroom_index,
    ).first()
    if not save:
        return {
            "RoomId": room_id,
            "SubroomIndex": subroom_index,
            "SaveIndex": save_index,
            "SaveVersion": 0,
            "SaveData": "{}",
        }
    return {
        "RoomId": room_id,
        "SubroomIndex": subroom_index,
        "SaveIndex": save_index,
        "SaveVersion": save.save_version,
        "SaveData": save.save_data,
    }


class SaveRoomDataBody(BaseModel):
    subroomIndex: int = 0
    saveIndex: int = 0
    saveData: str = "{}"


@router.put("/rooms/{room_id}/subrooms/{subroom_index}/saves/{save_index}")
async def save_subroom_data(
    room_id: str,
    subroom_index: int,
    save_index: int,
    body: SaveRoomDataBody,
    user: User = Depends(get_current_user),
    db: Session = Depends(get_db),
):
    save = db.query(RoomSave).filter(
        RoomSave.room_id == room_id,
        RoomSave.subroom_index == subroom_index,
    ).first()
    if not save:
        save = RoomSave(
            room_id=room_id,
            subroom_index=subroom_index,
            save_data=body.saveData,
            save_version=1,
        )
        db.add(save)
    else:
        save.save_data = body.saveData
        save.save_version += 1
        save.updated_at = datetime.now(timezone.utc)
    db.commit()
    return {"status": "ok", "save_version": save.save_version}


@router.get("/photon_access_token")
async def photon_access_token(user: User = Depends(get_current_user)):
    return {
        "AccessToken": str(uuid.uuid4()),
        "UserId": user.id,
        "Username": user.username,
        "DisplayName": user.display_name or user.username,
    }


@router.get("/publishState/configs")
async def publish_state_configs():
    return {
        "MaxPublishedRoomCount": 50,
        "MaxPublishedRoomVersionCount": 100,
        "RequirePhoto": False,
        "RequirePhotoScore": False,
    }


@router.get("/api/rooms/v1/filters")
async def room_filters():
    return {
        "Filters": [
            {"Id": "hot", "Name": "Hot", "SortOrder": 0},
            {"Id": "new", "Name": "New", "SortOrder": 1},
            {"Id": "featured", "Name": "Featured", "SortOrder": 2},
            {"Id": "subscribers", "Name": "Subscribers", "SortOrder": 3},
            {"Id": "friends", "Name": "Friends", "SortOrder": 4},
        ]
    }


@router.post("/api/rooms/v1/verifyRole")
async def verify_role():
    return {"IsVerified": True, "Role": "member"}


@router.post("/api/quickPlay/v1/getandclear")
async def quickplay_getandclear():
    return []


@router.get("/api/customAvatarItems/v1/isRenderingEnabled")
async def avatar_rendering_enabled():
    return True


@router.get("/api/customAvatarItems/v1/isCreationEnabled")
async def avatar_creation_enabled():
    return True


@router.get("/api/customAvatarItems/v1/isCreationAllowedForAccount")
async def avatar_creation_allowed(user: User = Depends(get_current_user)):
    return True


@router.post("/api/customAvatarItems/v1/bulk")
async def avatar_items_bulk():
    return []


@router.get("/api/customAvatarItems/v1/owned")
async def avatar_items_owned(skip: int = 0, take: int = 1000):
    return []


@router.get("/outfits/me/saved")
async def saved_outfits(user: User = Depends(get_current_user), db: Session = Depends(get_db)):
    outfits = db.query(Outfit).filter(Outfit.user_id == user.id).all()
    return [
        {
            "Id": o.id,
            "OutfitName": o.outfit_name,
            "OutfitData": json.loads(o.outfit_data) if o.outfit_data else {},
            "IsSaved": o.is_saved,
        }
        for o in outfits
    ]


class SaveOutfitBody(BaseModel):
    outfitName: str
    outfitData: dict = {}


@router.post("/outfits/me/saved")
async def save_outfit(
    body: SaveOutfitBody,
    user: User = Depends(get_current_user),
    db: Session = Depends(get_db),
):
    outfit = Outfit(
        user_id=user.id,
        outfit_name=body.outfitName,
        outfit_data=json.dumps(body.outfitData),
    )
    db.add(outfit)
    db.commit()
    return {"status": "ok", "id": outfit.id}


@router.get("/api/inventions/v2/mine")
async def my_inventions(user: User = Depends(get_current_user)):
    return []


@router.get("/api/inventions/v1/room")
async def room_inventions(id: str = ""):
    return []
