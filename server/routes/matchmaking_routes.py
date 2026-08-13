import uuid
from datetime import datetime, timezone

from fastapi import APIRouter, Depends
from pydantic import BaseModel
from sqlalchemy.orm import Session

from ..database import get_db, User, PlayerState, Room
from .auth_routes import get_current_user

router = APIRouter()


def new_legacy_room_id() -> str:
    return str((uuid.uuid4().int % 8_000_000_000_000_000_000) + 1)


def new_instance_id() -> int:
    """RoomInstanceId is an Int64 on the client, not a GUID string."""
    return (uuid.uuid4().int % 8_000_000_000_000_000_000) + 1


# Photon region the client should join. The plugin pins Photon's FixedRegion to
# "us", so this has to agree with it or the client joins a room in one region
# while talking to the nameserver for another.
PHOTON_REGION = "us"


def photon_room_id(room_id: int | str, instance_id: int) -> str:
    """The Photon Cloud room name every client in this instance must agree on.

    Deriving it from room + instance (rather than randomly) means two clients
    matchmaking into the same instance land in the same Photon room, which is
    what makes this actually multiplayer.
    """
    return f"rr-{room_id}-{instance_id}"


def room_instance(
    room_id: int | str,
    name: str,
    instance_id: int | None = None,
    *,
    max_capacity: int = 40,
    is_private: bool = False,
    room_instance_type: int = 0,
    sub_room_id: int = 0,
) -> dict:
    """The client's room-instance object.

    Field names and types recovered from the generated formatter JKPFPPJNLJE
    (which deserializes OAILMIHJFAK) in this exact GameAssembly - see
    il2cpp-dump. The formatter accepts PascalCase, camelCase and lowercase, so
    PascalCase is used here. Do NOT invent extra fields or rename these: the
    client drives Photon entirely from PhotonRoomId + PhotonRegionId, and a
    shape it cannot deserialize is why matchmaking previously did nothing.
    """
    if instance_id is None:
        instance_id = new_instance_id()
    try:
        numeric_room_id: int | str = int(room_id)
    except (TypeError, ValueError):
        numeric_room_id = room_id

    return {
        "RoomId": numeric_room_id,
        "RoomInstanceId": instance_id,
        "RoomInstanceType": room_instance_type,
        "PhotonRoomId": photon_room_id(numeric_room_id, instance_id),
        "PhotonRegionId": PHOTON_REGION,
        "Name": name,
        "Location": None,
        "SubRoomId": sub_room_id,
        "RoomCode": "",
        "MaxCapacity": max_capacity,
        "EventId": None,
        "ClubId": None,
        "IsPrivate": is_private,
        "IsFull": False,
        "IsInProgress": True,
        "EncryptVoiceChat": False,
    }


def matchmaking_response(instance: dict) -> dict:
    """The matchmaking result object (AGHDLKGKLGK, formatter DLFNHIEOGPG).

    Exactly three properties: RoomInstanceId (Int64), PhotonAccessToken
    (String) and Permissions (list). The room instance is included alongside so
    clients that read either shape can find the Photon details.
    """
    return {
        "RoomInstanceId": instance["RoomInstanceId"],
        "PhotonAccessToken": uuid.uuid4().hex,
        "Permissions": [],
        "RoomInstance": instance,
        **instance,
    }


@router.get("/player")
async def get_player(
    id: str | None = None,
    user: User = Depends(get_current_user),
    db: Session = Depends(get_db),
):
    target_id = id or user.id
    state = db.query(PlayerState).filter(PlayerState.user_id == target_id).first()
    return {
        "Id": target_id,
        "IsOnline": state.is_online if state else False,
        "CurrentRoomId": state.current_room_id if state else None,
        "CurrentInstanceId": state.current_instance_id if state else None,
        "LastRoomId": state.last_room_id if state else None,
    }


class PlayerLoginBody(BaseModel):
    platform: str = "steam"
    platformId: str | None = None
    gameVersion: str = "20260323.14"


@router.post("/player/login")
async def player_login(
    user: User = Depends(get_current_user),
    db: Session = Depends(get_db),
):
    state = db.query(PlayerState).filter(PlayerState.user_id == user.id).first()
    if not state:
        state = PlayerState(user_id=user.id)
        db.add(state)
    state.is_online = True
    state.last_activity = datetime.now(timezone.utc)
    db.commit()

    return {
        "Id": user.id,
        "IsOnline": True,
        "CurrentRoomId": None,
        "LastActivity": state.last_activity.isoformat() if state.last_activity else None,
        "SessionId": str(uuid.uuid4()),
    }


@router.post("/player/logout")
async def player_logout(
    user: User = Depends(get_current_user),
    db: Session = Depends(get_db),
):
    state = db.query(PlayerState).filter(PlayerState.user_id == user.id).first()
    if state:
        state.is_online = False
        state.last_room_id = state.current_room_id
        state.current_room_id = None
        state.current_instance_id = None
        db.commit()
    return {"status": "ok"}


class HeartbeatBody(BaseModel):
    roomId: str | None = None
    instanceId: str | None = None


@router.post("/player/heartbeat")
async def player_heartbeat(
    body: HeartbeatBody,
    user: User = Depends(get_current_user),
    db: Session = Depends(get_db),
):
    state = db.query(PlayerState).filter(PlayerState.user_id == user.id).first()
    if not state:
        state = PlayerState(user_id=user.id)
        db.add(state)
    state.is_online = True
    state.last_activity = datetime.now(timezone.utc)
    if body.roomId:
        state.current_room_id = body.roomId
        state.last_room_id = body.roomId
    if body.instanceId:
        state.current_instance_id = body.instanceId
    db.commit()
    return {"status": "ok", "lastActivity": state.last_activity.isoformat()}


@router.get("/player/qos")
async def player_qos():
    return {
        "Region": "us",
        "QosServers": [
            {"Region": "us", "Address": "127.0.0.1", "Port": 3074, "LatencyMs": 5},
            {"Region": "eu", "Address": "127.0.0.1", "Port": 3074, "LatencyMs": 80},
            {"Region": "asia", "Address": "127.0.0.1", "Port": 3074, "LatencyMs": 150},
        ],
    }


@router.get("/player/connection-info")
async def connection_info(user: User = Depends(get_current_user)):
    return {
        "UserId": user.id,
        "ConnectionString": "recnet://127.0.0.1:8081",
        "Region": "us",
    }


@router.post("/player/exclusivelogin")
async def exclusive_login(user: User = Depends(get_current_user)):
    return {"status": "ok", "isExclusive": True}


@router.put("/player/gameserverregionpings")
async def region_pings():
    return {"status": "ok"}


@router.put("/player/statusvisibility")
async def status_visibility(user: User = Depends(get_current_user)):
    return {"status": "ok"}


@router.post("/matchmake/dorm")
async def matchmake_dorm(
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

    instance_id = new_instance_id()
    state = db.query(PlayerState).filter(PlayerState.user_id == user.id).first()
    if state:
        state.current_room_id = room.room_id
        state.current_instance_id = str(instance_id)
        state.last_activity = datetime.now(timezone.utc)
        db.commit()

    return matchmaking_response(
        room_instance(
            room.room_id,
            room.name,
            instance_id,
            max_capacity=room.capacity or 40,
            is_private=True,
        )
    )


@router.post("/matchmake/v2/room/{room_id}")
async def matchmake_room(
    room_id: str,
    user: User = Depends(get_current_user),
    db: Session = Depends(get_db),
):
    instance_id = new_instance_id()

    # RoomId -2 is the bundled offline Orientation room; it has no database row.
    if str(room_id) == "-2":
        return matchmaking_response(
            room_instance(
                -2,
                "Orientation",
                instance_id,
                max_capacity=1,
                is_private=True,
            )
        )

    room = db.query(Room).filter(Room.room_id == room_id).first()
    if not room:
        return {"error": "Room not found", "RoomId": room_id}

    state = db.query(PlayerState).filter(PlayerState.user_id == user.id).first()
    if state:
        state.current_room_id = room.room_id
        state.current_instance_id = str(instance_id)
        state.last_activity = datetime.now(timezone.utc)
        db.commit()

    room.player_count += 1
    db.commit()

    return matchmaking_response(
        room_instance(
            room.room_id,
            room.name,
            instance_id,
            max_capacity=room.capacity or 40,
            is_private=room.is_private,
        )
    )


@router.post("/matchmake/room/{room_id}")
async def matchmake_room_v1(
    room_id: str,
    user: User = Depends(get_current_user),
    db: Session = Depends(get_db),
):
    return await matchmake_room(room_id, user, db)


@router.post("/matchmake/none")
async def matchmake_none(user: User = Depends(get_current_user)):
    """Called on the launch path for the bundled Orientation room.

    This previously returned {"status": "ok"}, which carries no Photon room, so
    the client had nothing to connect to - PhotonHandler would Awake and then
    immediately Disconnect, and with no Photon session the local player is never
    spawned (RpcSpawnNewPlayer needs a PhotonView). Return a real instance.
    """
    return matchmaking_response(
        room_instance(-2, "Orientation", max_capacity=1, is_private=True)
    )


@router.post("/roominstance/{instance_id}/reportjoinresult")
async def report_join_result(instance_id: str, user: User = Depends(get_current_user)):
    return {"status": "ok"}


@router.get("/rooms/requiring/developer")
async def rooms_requiring_developer():
    return []


@router.get("/rooms/requiring/rrplus")
async def rooms_requiring_rrplus():
    return []
