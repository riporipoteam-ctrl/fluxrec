"""Small Firebase Auth REST bridge used by the legacy Unity client.

The web API key identifies the public Firebase project; it is not an admin
credential. Every operation still requires the player's password or ID token.
No service-account key is stored in the game or local server.
"""

from __future__ import annotations

import asyncio
import json
from dataclasses import dataclass
from urllib.error import HTTPError, URLError
from urllib.request import Request, urlopen

from .config import FIREBASE_WEB_API_KEY

IDENTITY_BASE = "https://identitytoolkit.googleapis.com/v1"


class FirebaseAuthError(Exception):
    def __init__(self, code: str, message: str | None = None, status: int = 400):
        self.code = code
        self.status = status
        super().__init__(message or code.replace("_", " ").title())


@dataclass(frozen=True)
class FirebaseIdentity:
    uid: str
    email: str
    email_verified: bool
    display_name: str
    id_token: str
    created: bool = False


def is_firebase_enabled() -> bool:
    return bool(FIREBASE_WEB_API_KEY.strip())


def _post(endpoint: str, payload: dict) -> dict:
    if not is_firebase_enabled():
        raise FirebaseAuthError("FIREBASE_NOT_CONFIGURED", status=503)
    request = Request(
        f"{IDENTITY_BASE}/{endpoint}?key={FIREBASE_WEB_API_KEY}",
        data=json.dumps(payload).encode("utf-8"),
        headers={"Content-Type": "application/json"},
        method="POST",
    )
    try:
        with urlopen(request, timeout=10) as response:
            return json.loads(response.read().decode("utf-8"))
    except HTTPError as error:
        body = error.read().decode("utf-8", errors="replace")
        code = f"FIREBASE_HTTP_{error.code}"
        message = code
        try:
            detail = json.loads(body).get("error", {})
            raw = str(detail.get("message") or code)
            code = raw.split(" : ", 1)[0].strip()
            message = raw
        except (ValueError, TypeError):
            pass
        raise FirebaseAuthError(code, message, error.code) from error
    except (URLError, TimeoutError, OSError) as error:
        raise FirebaseAuthError(
            "FIREBASE_UNAVAILABLE",
            "Flux account service is temporarily unavailable.",
            503,
        ) from error


async def _post_async(endpoint: str, payload: dict) -> dict:
    return await asyncio.to_thread(_post, endpoint, payload)


async def lookup_id_token(id_token: str) -> FirebaseIdentity:
    result = await _post_async("accounts:lookup", {"idToken": id_token})
    users = result.get("users") or []
    if not users:
        raise FirebaseAuthError("INVALID_ID_TOKEN", status=401)
    user = users[0]
    return FirebaseIdentity(
        uid=str(user.get("localId") or ""),
        email=str(user.get("email") or "").strip().lower(),
        email_verified=bool(user.get("emailVerified")),
        display_name=str(user.get("displayName") or "").strip(),
        id_token=id_token,
    )


async def sign_in(email: str, password: str) -> FirebaseIdentity:
    result = await _post_async(
        "accounts:signInWithPassword",
        {
            "email": email.strip().lower(),
            "password": password,
            "returnSecureToken": True,
        },
    )
    identity = await lookup_id_token(str(result.get("idToken") or ""))
    return FirebaseIdentity(
        **{**identity.__dict__, "created": False},
    )


async def sign_up(email: str, password: str, display_name: str) -> FirebaseIdentity:
    result = await _post_async(
        "accounts:signUp",
        {
            "email": email.strip().lower(),
            "password": password,
            "returnSecureToken": True,
        },
    )
    id_token = str(result.get("idToken") or "")
    if display_name:
        update = await _post_async(
            "accounts:update",
            {
                "idToken": id_token,
                "displayName": display_name[:32],
                "returnSecureToken": True,
            },
        )
        id_token = str(update.get("idToken") or id_token)
    identity = await lookup_id_token(id_token)
    return FirebaseIdentity(
        **{**identity.__dict__, "created": True},
    )


async def send_verification_email(id_token: str) -> None:
    await _post_async(
        "accounts:sendOobCode",
        {"requestType": "VERIFY_EMAIL", "idToken": id_token},
    )
