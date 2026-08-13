import uuid
import hashlib
import re
import secrets
from datetime import datetime, timedelta, timezone

import jwt
from fastapi import APIRouter, Depends, HTTPException, Request, Response
from fastapi.security import HTTPBearer, HTTPAuthorizationCredentials
from pydantic import BaseModel
from sqlalchemy import func
from sqlalchemy.orm import Session

from ..database import get_db, User, PlayerState, PlayerSettings, Room
from ..config import SECRET_KEY, ACCESS_TOKEN_EXPIRE_MINUTES, ADMIN_EMAIL
from ..firebase_auth import (
    FirebaseAuthError,
    FirebaseIdentity,
    is_firebase_enabled,
    lookup_id_token,
    send_verification_email,
    sign_in as firebase_sign_in,
    sign_up as firebase_sign_up,
)

router = APIRouter()
security = HTTPBearer(auto_error=False)
TOKEN_SCOPE = (
    "offline_access profile rn rn.accounts rn.accounts.gc rn.api rn.chat "
    "rn.clubs rn.commerce rn.match.read rn.match.write rn.notify rn.rooms rn.storage"
)
TOKEN_KEY = "8oQ+e+WQaOBPbEcakhqs3dwZZdOmmyDUmJSD9u4AHMY="
async def read_auth_body(request: Request) -> dict:
    content_type = request.headers.get("content-type", "").lower()
    try:
        if "application/json" in content_type:
            return dict(await request.json())
        if "application/x-www-form-urlencoded" in content_type or "multipart/form-data" in content_type:
            return dict(await request.form())
    except Exception:
        return {}
    return {}


def auth_value(body: dict, *names: str, default=""):
    values = {str(key).lower(): value for key, value in body.items()}
    for name in names:
        value = values.get(name.lower())
        if value is not None:
            return value
    return default


def hash_password(password: str) -> str:
    salt = secrets.token_hex(16)
    pwd_hash = hashlib.pbkdf2_hmac("sha256", password.encode(), salt.encode(), 100000).hex()
    return f"{salt}${pwd_hash}"


def verify_password(password: str, stored: str) -> bool:
    parts = stored.split("$")
    if len(parts) != 2:
        return False
    salt, pwd_hash = parts
    computed = hashlib.pbkdf2_hmac("sha256", password.encode(), salt.encode(), 100000).hex()
    return computed == pwd_hash


def create_token(user: User) -> str:
    payload = {
        "sub": user.id,
        "username": user.username,
        "iat": datetime.now(timezone.utc),
        "exp": datetime.now(timezone.utc) + timedelta(minutes=ACCESS_TOKEN_EXPIRE_MINUTES),
    }
    return jwt.encode(payload, SECRET_KEY, algorithm="HS256")


def next_legacy_account_id(db: Session) -> str:
    numeric_ids = [
        int(row[0])
        for row in db.query(User.id).all()
        if row[0] is not None and str(row[0]).isdigit()
    ]
    return str(max(numeric_ids, default=999) + 1)


def client_account_id(user: User):
    return int(user.id) if str(user.id).isdigit() else user.id


def is_verified_admin(email, email_verified: bool) -> bool:
    return (
        bool(email_verified)
        and bool(ADMIN_EMAIL)
        and str(email or "").strip().casefold() == ADMIN_EMAIL
    )


def find_user_by_email(db: Session, email: str) -> User | None:
    normalized = str(email or "").strip().casefold()
    if not normalized:
        return None
    return (
        db.query(User)
        .filter(func.lower(User.email) == normalized)
        .first()
    )


def unique_game_username(db: Session, requested: str) -> str:
    base = re.sub(r"[^a-zA-Z0-9_]", "", requested or "").strip("_")[:24]
    if len(base) < 3:
        base = "fluxplayer"
    candidate = base
    suffix = 1
    while (
        db.query(User)
        .filter(func.lower(User.username) == candidate.casefold())
        .first()
    ):
        suffix += 1
        candidate = f"{base[: max(3, 24 - len(str(suffix)))]}{suffix}"
    return candidate


def update_firebase_link(user: User, identity: FirebaseIdentity, password: str | None = None):
    if user.firebase_uid and user.firebase_uid != identity.uid:
        raise HTTPException(status_code=409, detail="Flux identity is already linked")
    user.firebase_uid = identity.uid
    user.email = identity.email
    user.email_verified = identity.email_verified
    user.is_developer = is_verified_admin(identity.email, identity.email_verified)
    if identity.display_name and not user.display_name:
        user.display_name = identity.display_name[:32]
    if password:
        # Keep a salted local fallback for this PC. Firebase remains the source
        # of truth whenever the linked account service is reachable.
        user.password_hash = hash_password(password)


def firebase_http_error(error: FirebaseAuthError) -> HTTPException:
    invalid = {
        "EMAIL_NOT_FOUND",
        "INVALID_PASSWORD",
        "INVALID_LOGIN_CREDENTIALS",
        "USER_DISABLED",
    }
    if error.code in invalid:
        return HTTPException(status_code=401, detail="Invalid Flux account credentials")
    if error.code == "EMAIL_EXISTS":
        return HTTPException(status_code=409, detail="Flux email is already registered")
    if error.code == "OPERATION_NOT_ALLOWED":
        return HTTPException(
            status_code=503,
            detail="Firebase email/password authentication is not enabled",
        )
    return HTTPException(
        status_code=503 if error.status >= 500 else 400,
        detail=str(error),
    )


def ensure_player_records(db: Session, user: User):
    settings = db.query(PlayerSettings).filter(PlayerSettings.user_id == user.id).first()
    if not settings:
        db.add(PlayerSettings(user_id=user.id))
    state = db.query(PlayerState).filter(PlayerState.user_id == user.id).first()
    if not state:
        db.add(PlayerState(user_id=user.id, is_online=True))
    else:
        state.is_online = True
        state.last_activity = datetime.now(timezone.utc)


def extract_bearer_token(request: Request) -> str:
    """Pull the access token out of a request as tolerantly as possible.

    FastAPI's HTTPBearer only accepts a literal "Bearer <token>" Authorization
    header and returns nothing otherwise. The native client does not always
    present it that way, and a request that carried a perfectly valid token was
    being rejected with 401 - which broke the flow immediately after a
    successful /connect/token. Accept the common variants instead.
    """
    header = (
        request.headers.get("authorization")
        or request.headers.get("Authorization")
        or ""
    ).strip()

    if header:
        parts = header.split(None, 1)
        if len(parts) == 2 and parts[0].lower() in {"bearer", "token", "jwt"}:
            return parts[1].strip()
        # A bare token with no scheme at all.
        if len(parts) == 1:
            return parts[0].strip()

    # Some call sites pass it as a query parameter instead.
    for key in ("access_token", "accessToken", "token"):
        value = request.query_params.get(key)
        if value:
            return value.strip()

    return ""


def user_from_token(token: str, db: Session) -> User | None:
    if not token:
        return None
    try:
        payload = jwt.decode(token, SECRET_KEY, algorithms=["HS256"])
    except jwt.InvalidTokenError:
        return None
    user_id = payload.get("sub")
    if user_id is None:
        return None
    return db.query(User).filter(User.id == str(user_id)).first()


def get_current_user(
    request: Request,
    db: Session = Depends(get_db),
) -> User:
    user = user_from_token(extract_bearer_token(request), db)
    if user is not None:
        return user
    raise HTTPException(status_code=401, detail="Authorization required")


def get_optional_user(
    request: Request,
    db: Session = Depends(get_db),
) -> User | None:
    return user_from_token(extract_bearer_token(request), db)


@router.get("/eac/challenge")
async def eac_challenge():
    # This legacy client expects a JSON-quoted base64 string served as plain
    # text. Returning the generic JSON fallback object makes LoginHelper abort
    # before it posts the password grant to /connect/token.
    return Response(content='"AA=="', media_type="text/plain")


def token_payload(user: User) -> dict:
    """The shape the client accepts for a successful token exchange.

    Duplicated casings are deliberate: different call sites in the client read
    different casings of the same value.
    """
    token = create_token(user)
    display_name = user.display_name or user.username
    now = datetime.now(timezone.utc)
    return {
        "access_token": token,
        "accessToken": token,
        "AccessToken": token,
        "Token": token,
        "token_type": "Bearer",
        "expires_in": ACCESS_TOKEN_EXPIRE_MINUTES * 60,
        "scope": TOKEN_SCOPE,
        "key": TOKEN_KEY,
        "username": user.username,
        "Username": user.username,
        "account_id": client_account_id(user),
        "AccountId": client_account_id(user),
        "AccountID": client_account_id(user),
        "UserId": client_account_id(user),
        "PlayerId": client_account_id(user),
        "display_name": display_name,
        "DisplayName": display_name,
        "refresh_token": token,
        "refreshToken": token,
        "RefreshToken": token,
        ".issued": now.isoformat(),
        ".expires": (now + timedelta(minutes=ACCESS_TOKEN_EXPIRE_MINUTES)).isoformat(),
    }


def platform_token_response(db: Session, body: dict, platform_id: str) -> dict:
    """Log in (or silently register) an account keyed by platform id.

    The platform ticket is not validated: this is a self-hosted server with no
    way to verify a Steam ticket, and the client only needs a usable session.
    """
    marker = f"steam:{platform_id}"
    user = (
        db.query(User)
        .filter(func.lower(User.platform_id) == marker.casefold())
        .first()
        if hasattr(User, "platform_id")
        else None
    )

    if user is None:
        # Fall back to a deterministic username so repeat launches reuse the
        # same account instead of creating a new one every time.
        generated = f"player{platform_id[-8:]}" if platform_id else "player"
        user = (
            db.query(User)
            .filter(func.lower(User.username) == generated.casefold())
            .first()
        )
        if user is None:
            user = User(
                id=next_legacy_account_id(db),
                username=generated,
                display_name=generated,
                password_hash=hash_password(secrets.token_hex(16)),
            )
            if hasattr(User, "platform_id"):
                user.platform_id = marker
            db.add(user)
            db.commit()
            db.refresh(user)
        elif hasattr(User, "platform_id") and not user.platform_id:
            user.platform_id = marker
            db.commit()

    ensure_player_records(db, user)
    db.commit()
    return token_payload(user)


@router.post("/connect/token")
@router.post("/login")
@router.post("/account/login")
@router.post("/api/account/login")
@router.post("/api/auth/login")
async def connect_token(request: Request, db: Session = Depends(get_db)):
    body = await read_auth_body(request)
    username = str(
        auth_value(body, "username", "accountName", "login", "email")
    ).strip()
    password = auth_value(body, "password", "pass", "credential")
    account_id = auth_value(body, "account_id", "accountId")

    # The native client does NOT use the password grant. On launch it posts
    #   grant_type=create_account&client_id=recroom&platform=0
    #   &platform_id=<steamid>&ver=20230302&platform_auth={"Ticket":...}
    #   &eac_challenge=..&eac_response=..&build_key=..&dinfo=..
    # and it has no username/password to give. Rejecting that with 400
    # ("username and password required") is what produced
    #   "RecNet login failed. StatusCode: 400"
    # in the client, which left it with no session - so matchmaking never ran,
    # Photon never connected, and the local player was never spawned.
    # Treat the platform identity as the credential and mint a real token.
    grant_type = str(auth_value(body, "grant_type")).strip().casefold()
    platform_id = str(auth_value(body, "platform_id", "platformId")).strip()
    if grant_type in {"create_account", "platform", "platform_login", "steam"} and platform_id:
        return platform_token_response(db, body, platform_id)

    if (not username and not account_id) or not password:
        raise HTTPException(status_code=400, detail="username and password required")

    if username:
        user = (
            db.query(User)
            .filter(func.lower(User.username) == username.casefold())
            .first()
        )
        if user is None and "@" in username:
            user = find_user_by_email(db, username)
    else:
        user = db.query(User).filter(User.id == str(account_id).strip()).first()

    firebase_identity: FirebaseIdentity | None = None
    firebase_login_email = (
        user.email if user is not None and user.email else username if "@" in username else ""
    )
    should_use_firebase = bool(
        is_firebase_enabled()
        and firebase_login_email
        and (user is None or user.firebase_uid or "@" in username)
    )
    if should_use_firebase:
        try:
            firebase_identity = await firebase_sign_in(firebase_login_email, str(password))
        except FirebaseAuthError as error:
            # An unlinked legacy local account remains usable if it does not
            # exist in Firebase. Linked accounts never bypass Firebase errors.
            local_ok = user is not None and verify_password(str(password), user.password_hash)
            if user is None or user.firebase_uid or not local_ok:
                raise firebase_http_error(error) from error

    if firebase_identity:
        linked = (
            db.query(User)
            .filter(User.firebase_uid == firebase_identity.uid)
            .first()
        )
        if user is None:
            user = linked or find_user_by_email(db, firebase_identity.email)
        elif linked is not None and linked.id != user.id:
            raise HTTPException(status_code=409, detail="Flux identity is already linked")
        if user is None:
            requested = firebase_identity.display_name or firebase_identity.email.split("@")[0]
            user = User(
                id=next_legacy_account_id(db),
                username=unique_game_username(db, requested),
                display_name=firebase_identity.display_name or requested,
                password_hash=hash_password(str(password)),
            )
            db.add(user)
            db.flush()
        update_firebase_link(user, firebase_identity, str(password))
    elif user is None or not verify_password(str(password), user.password_hash):
        raise HTTPException(status_code=401, detail="Invalid credentials")

    user.last_login = datetime.now(timezone.utc)
    user.is_developer = is_verified_admin(user.email, user.email_verified)
    ensure_player_records(db, user)
    db.commit()

    token = create_token(user)
    display_name = user.display_name or user.username
    return {
        "access_token": token,
        "accessToken": token,
        "AccessToken": token,
        "Token": token,
        "token_type": "Bearer",
        "expires_in": ACCESS_TOKEN_EXPIRE_MINUTES * 60,
        "scope": TOKEN_SCOPE,
        "key": TOKEN_KEY,
        "username": user.username,
        "Username": user.username,
        "account_id": client_account_id(user),
        "AccountId": client_account_id(user),
        "AccountID": client_account_id(user),
        "UserId": client_account_id(user),
        "PlayerId": client_account_id(user),
        "display_name": display_name,
        "DisplayName": display_name,
        "refresh_token": token,
        "refreshToken": token,
        "RefreshToken": token,
        ".issued": datetime.now(timezone.utc).isoformat(),
        ".expires": (datetime.now(timezone.utc) + timedelta(minutes=ACCESS_TOKEN_EXPIRE_MINUTES)).isoformat(),
    }


@router.post("/cachedlogin/forplatformids")
async def cached_login_for_platforms(request: Request, db: Session = Depends(get_db)):
    body = await request.json()
    platform_id = body.get("platformId", body.get("platform_id", ""))
    platform = body.get("platform", "steam")

    user = db.query(User).filter(User.id == platform_id).first()
    if not user:
        user = db.query(User).filter(User.username == platform_id).first()

    if not user:
        user = User(
            id=next_legacy_account_id(db),
            username=f"player_{platform_id[:8]}",
            display_name=f"Player_{platform_id[:8]}",
            password_hash=hash_password(str(uuid.uuid4())),
        )
        db.add(user)
        db.commit()
        db.refresh(user)

    token = create_token(user)
    display_name = user.display_name or user.username
    return {
        "access_token": token,
        "accessToken": token,
        "AccessToken": token,
        "Token": token,
        "token_type": "Bearer",
        "expires_in": ACCESS_TOKEN_EXPIRE_MINUTES * 60,
        "scope": TOKEN_SCOPE,
        "key": TOKEN_KEY,
        "username": user.username,
        "Username": user.username,
        "account_id": client_account_id(user),
        "AccountId": client_account_id(user),
        "AccountID": client_account_id(user),
        "UserId": client_account_id(user),
        "PlayerId": client_account_id(user),
        "display_name": display_name,
        "DisplayName": display_name,
        "refresh_token": token,
        "refreshToken": token,
        "RefreshToken": token,
    }


@router.get("/role/developer")
async def developer_role(user: User = Depends(get_current_user)):
    return {"IsDeveloper": user.is_developer}


@router.post("/signup")
@router.post("/register")
@router.post("/account/register")
@router.post("/api/account/register")
@router.post("/api/auth/signup")
@router.post("/api/accounts/v1/register")
async def signup(request: Request, db: Session = Depends(get_db)):
    body = await read_auth_body(request)
    username = str(auth_value(body, "username", "accountName", "userName")).strip()
    password = str(auth_value(body, "password", "pass", "credential"))
    display_name = str(auth_value(body, "displayName", "display_name", default=username)).strip()
    email = str(auth_value(body, "email", "emailAddress", default="") or "").strip().lower()

    if not username or not password:
        raise HTTPException(status_code=400, detail="username and password required")

    existing = (
        db.query(User)
        .filter(func.lower(User.username) == username.casefold())
        .first()
    )
    if existing:
        raise HTTPException(status_code=409, detail="Username already taken")

    firebase_identity: FirebaseIdentity | None = None
    if email and is_firebase_enabled():
        try:
            firebase_identity = await firebase_sign_up(email, password, display_name or username)
        except FirebaseAuthError as error:
            if error.code != "EMAIL_EXISTS":
                raise firebase_http_error(error) from error
            # Creating a game profile for an existing Flux web account is
            # allowed only after proving ownership with the same password.
            try:
                firebase_identity = await firebase_sign_in(email, password)
            except FirebaseAuthError as sign_in_error:
                raise firebase_http_error(sign_in_error) from sign_in_error

        linked = (
            db.query(User)
            .filter(User.firebase_uid == firebase_identity.uid)
            .first()
        )
        if linked is not None or find_user_by_email(db, firebase_identity.email):
            raise HTTPException(
                status_code=409,
                detail="This Flux account already has a game profile; sign in instead",
            )

    user = User(
        id=next_legacy_account_id(db),
        username=username,
        display_name=display_name or username,
        password_hash=hash_password(password),
        email=email or None,
        firebase_uid=firebase_identity.uid if firebase_identity else None,
        email_verified=firebase_identity.email_verified if firebase_identity else False,
        is_developer=is_verified_admin(
            firebase_identity.email if firebase_identity else email,
            firebase_identity.email_verified if firebase_identity else False,
        ),
    )
    db.add(user)
    db.flush()
    ensure_player_records(db, user)
    db.commit()
    db.refresh(user)

    if firebase_identity and firebase_identity.created and not firebase_identity.email_verified:
        try:
            await send_verification_email(firebase_identity.id_token)
        except FirebaseAuthError:
            # The account exists and can play; verification can be requested
            # again from Flux without breaking game registration.
            pass

    token = create_token(user)
    resolved_display_name = user.display_name or user.username
    return {
        "access_token": token,
        "accessToken": token,
        "AccessToken": token,
        "Token": token,
        "token_type": "Bearer",
        "expires_in": ACCESS_TOKEN_EXPIRE_MINUTES * 60,
        "scope": TOKEN_SCOPE,
        "key": TOKEN_KEY,
        "username": user.username,
        "Username": user.username,
        "account_id": client_account_id(user),
        "AccountId": client_account_id(user),
        "AccountID": client_account_id(user),
        "UserId": client_account_id(user),
        "PlayerId": client_account_id(user),
        "display_name": resolved_display_name,
        "DisplayName": resolved_display_name,
        "refresh_token": token,
        "refreshToken": token,
        "RefreshToken": token,
    }


class SignUpRequest(BaseModel):
    username: str
    password: str
    displayName: str | None = None
    email: str | None = None


class FluxLinkRequest(BaseModel):
    idToken: str
    username: str | None = None
    displayName: str | None = None


@router.post("/api/flux/link")
async def link_flux_identity(payload: FluxLinkRequest, db: Session = Depends(get_db)):
    try:
        identity = await lookup_id_token(payload.idToken)
    except FirebaseAuthError as error:
        raise firebase_http_error(error) from error

    user = (
        db.query(User)
        .filter(User.firebase_uid == identity.uid)
        .first()
    ) or find_user_by_email(db, identity.email)

    requested_username = (
        payload.username
        or identity.display_name
        or identity.email.split("@")[0]
    )
    if user is None:
        username = unique_game_username(db, requested_username)
        user = User(
            id=next_legacy_account_id(db),
            username=username,
            display_name=payload.displayName or identity.display_name or username,
            password_hash=hash_password(secrets.token_urlsafe(32)),
        )
        db.add(user)
        db.flush()

    update_firebase_link(user, identity)
    if payload.displayName:
        user.display_name = payload.displayName[:32]
    ensure_player_records(db, user)
    db.commit()
    db.refresh(user)
    return {
        "linked": True,
        "accountId": client_account_id(user),
        "username": user.username,
        "displayName": user.display_name or user.username,
        "email": user.email,
        "emailVerified": bool(user.email_verified),
        "isAdmin": bool(user.is_developer),
    }


@router.post("/api/flux/rooms")
async def list_flux_rooms(payload: FluxLinkRequest, db: Session = Depends(get_db)):
    try:
        identity = await lookup_id_token(payload.idToken)
    except FirebaseAuthError as error:
        raise firebase_http_error(error) from error

    user = (
        db.query(User)
        .filter(User.firebase_uid == identity.uid)
        .first()
    )
    if user is None:
        raise HTTPException(
            status_code=404,
            detail="Link this Flux account to the game before syncing rooms",
        )

    rooms = db.query(Room).filter(Room.owner_id == user.id).all()
    return {
        "rooms": [
            {
                "roomId": room.room_id,
                "name": room.name,
                "description": room.description or "",
                "imageUrl": room.image_url,
                "tags": [
                    tag.strip()
                    for tag in re.sub(r"[\[\]\"]", "", room.tags or "")
                    .split(",")
                    if tag.strip()
                ],
                "isPrivate": bool(room.is_private),
                "visits": 0,
                "updatedAt": (
                    room.updated_at.isoformat()
                    if room.updated_at is not None
                    else None
                ),
            }
            for room in rooms
        ]
    }
