import os
from typing import Any

from fastapi import FastAPI, Request
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import JSONResponse, Response

from .config import PUBLIC_BASE_URL, PHOTON_APP_ID, FLUX_ALLOWED_ORIGINS
from .database import init_db
from .routes import (
    auth_routes,
    account_routes,
    room_routes,
    matchmaking_routes,
    economy_routes,
    social_routes,
    photon_routes,
    settings_routes,
    notifications_routes,
    misc_routes,
)

app = FastAPI(title="OpenRec Local RecNet")
app.add_middleware(
    CORSMiddleware,
    allow_origins=FLUX_ALLOWED_ORIGINS,
    # Netlify deploy previews and the final *.netlify.app site call the
    # player's localhost backend directly from their browser.
    allow_origin_regex=r"^https://[a-z0-9-]+\.netlify\.app$",
    allow_credentials=False,
    allow_methods=["GET", "POST", "OPTIONS"],
    allow_headers=["Authorization", "Content-Type"],
)
REQUEST_LOG_PATH = os.environ.get(
    "OPENREC_REQUEST_LOG",
    "",
).strip() or None


def local_url(path: str = "") -> str:
    base = f"{PUBLIC_BASE_URL.rstrip('/')}/"
    return f"{base}{path.strip('/')}/" if path else base


def official_url(host: str) -> str:
    """Keep the service identity the client expects; the plugin redirects it locally."""
    return f"https://{host}"


# This is deserialized straight into HEEMOONFCAF.NameServerResponse, which declares
# EXACTLY these 27 properties - no more, no less. Keep it that way.
#
# Two separate bugs lived here. API and Accounts were missing, and BPIFHBEBGHO.API is
# the base service nearly every RecNet call resolves against. Worse, the map also
# carried nine keys with no matching property (Cards, CMS, Data, Geo, Lists,
# RoomieIntegrations, StringsCDN, Thorn, Videos); the client's deserializer rejects
# the whole object on an unknown member, so the service dictionary came out empty and
# every lookup threw KeyNotFound. That is what stopped the client issuing any service
# request at all, which in turn killed player settings, matchmaking, the loading
# screen and the entire room-loading pipeline.
#
# Adding a key here that NameServerResponse does not declare will break the client
# again, silently. Verify against the interop type before touching this.
SERVICE_MAP: dict[str, Any] = {
    "RecNetStatus": official_url("api.rec.net"),
    "Auth": official_url("auth.rec.net"),
    "API": official_url("api.rec.net"),
    "WWW": official_url("rec.net"),
    "Notifications": official_url("notify.rec.net"),
    "Images": official_url("img.rec.net"),
    "CDN": official_url("cdn.rec.net"),
    "Commerce": official_url("commerce.rec.net"),
    "Matchmaking": official_url("match.rec.net"),
    "Storage": official_url("storage.rec.net"),
    "Chat": official_url("chat.rec.net"),
    "Leaderboard": official_url("leaderboard.rec.net"),
    "Accounts": official_url("accounts.rec.net"),
    "Link": official_url("link.rec.net"),
    "RoomComments": official_url("roomcomments.rec.net"),
    "Clubs": official_url("clubs.rec.net"),
    "Rooms": official_url("rooms.rec.net"),
    "PlatformNotifications": official_url("platformnotifications.rec.net"),
    "Moderation": official_url("moderation.rec.net"),
    "DataCollection": official_url("datacollection.rec.net"),
    "BugReporting": official_url("bugreporting.rec.net"),
    "Discovery": official_url("discovery.rec.net"),
    "PlayerSettings": official_url("playersettings.rec.net"),
    "Studio": official_url("studio.rec.net"),
    "GameLogs": official_url("gamelogs.rec.net"),
    "Strings": official_url("strings.rec.net"),
    "Econ": official_url("econ.rec.net"),
}


GAME_CONFIGS = [
    {"Key": "UseHeartbeatWebSocket", "Value": "0"},
    {"Key": "forceRegistration", "Value": "false"},
    {"Key": "Screens.ForceVerification", "Value": "false"},
    {"Key": "Photon.UsePhoton", "Value": "true"},
    {"Key": "Photon.AppId", "Value": PHOTON_APP_ID},
    {"Key": "Photon.Region", "Value": "us"},
    # This depot reports v=20230302 on its version check
    # (GET /api/versioncheck/v4?v=20230302&p=0&pid=0). The old value here was
    # 20260323.14, which belongs to the much newer Unity 6 build documented in
    # protocol/findings.md - not this client.
    {"Key": "RecNet.GameVersion", "Value": "20230302"},
    {"Key": "RecNet.Environment", "Value": "prod"},
    {"Key": "VoiceChat.Enabled", "Value": "true"},
    {"Key": "Multiplayer.MaxPlayers", "Value": "40"},
    {"Key": "Saving.Enabled", "Value": "true"},
    {"Key": "Accounts.AllowRegistration", "Value": "true"},
    {"Key": "Accounts.AllowGuestLogin", "Value": "true"},
    {"Key": "SandboxMode", "Value": "true"},
    {"Key": "UsePhotonCloud", "Value": "true"},
    {"Key": "LiveOps.TLA.IsEnabled", "Value": "false"},
]


VERSION_OK_V4 = {
    "VersionStatus": 0,
    "UpdateNotificationStage": 0,
    "IsVersionIslanded": False,
    "IsCrossPlayDisabled": False,
}


@app.on_event("startup")
async def startup():
    init_db()


@app.middleware("http")
async def log_requests(request: Request, call_next):
    line = f">>> {request.method} {request.url.path}?{request.url.query} host={request.headers.get('host')}"
    print(line, flush=True)
    if REQUEST_LOG_PATH:
        try:
            with open(REQUEST_LOG_PATH, "a", encoding="utf-8") as log:
                log.write(line + "\n")
        except OSError:
            pass
    response = await call_next(request)
    if REQUEST_LOG_PATH:
        try:
            with open(REQUEST_LOG_PATH, "a", encoding="utf-8") as log:
                log.write(f"<<< {request.method} {request.url.path} status={response.status_code}\n")
        except OSError:
            pass
    return response


@app.get("/api/versioncheck/v4")
async def version_check_v4(v: str | None = None, p: int | None = None):
    return VERSION_OK_V4


@app.head("/api/versioncheck/v4")
async def version_check_v4_head(v: str | None = None, p: int | None = None):
    # BestHTTP probes the nameserver with HEAD before accepting the service map.
    return JSONResponse(content=VERSION_OK_V4, status_code=200)


@app.get("/api/versioncheck/v3")
async def version_check_v3():
    return {"ValidVersion": True}


@app.get("/api/versioncheck/islandedversions")
async def islanded_versions():
    return []


@app.get("/api/gameconfigs/v1/all")
async def game_configs():
    return GAME_CONFIGS


@app.get("/config/{config_id}")
async def config_by_id(config_id: str):
    return {"Services": SERVICE_MAP, "GameConfigs": GAME_CONFIGS}


@app.post("/data/events")
async def data_events():
    return Response(status_code=204)


@app.get("/services")
async def services():
    return SERVICE_MAP


@app.get("/")
async def root_nameserver():
    return SERVICE_MAP


@app.get("/2")
async def root_nameserver_v2():
    return SERVICE_MAP


app.include_router(auth_routes.router)
app.include_router(account_routes.router)
app.include_router(room_routes.router)
app.include_router(matchmaking_routes.router)
app.include_router(economy_routes.router)
app.include_router(social_routes.router)
app.include_router(photon_routes.router)
app.include_router(settings_routes.router)
app.include_router(notifications_routes.router)
app.include_router(misc_routes.router)


@app.get("/api/sanitize/v1")
async def sanitize_get():
    return {"IsPure": True}


@app.post("/api/sanitize/v1")
async def sanitize_post():
    return {"IsClean": True, "SanitizedContent": ""}


@app.get("/api/v1/deviceId")
@app.post("/api/v1/deviceId")
async def device_id():
    return {
        "deviceId": "neorec-local-device",
        "duid": "neorec-duid-" + __import__("uuid").uuid4().hex[:16],
    }


@app.get("/PlayerReporting/v1/deviceId")
@app.post("/PlayerReporting/v1/deviceId")
async def device_id_legacy():
    return {
        "deviceId": "neorec-local-device",
        "duid": "neorec-duid-" + __import__("uuid").uuid4().hex[:16],
    }


@app.get("/{path:path}")
async def fallback_get(path: str):
    return JSONResponse(
        {
            "error": "unimplemented",
            "path": path,
            "hint": "This endpoint is not yet implemented. Check the logs for what the client is asking for.",
        },
        status_code=200,
    )


@app.post("/{path:path}")
async def fallback_post(path: str):
    return JSONResponse(
        {"error": "unimplemented", "path": path},
        status_code=200,
    )


@app.put("/{path:path}")
async def fallback_put(path: str):
    return JSONResponse(
        {"error": "unimplemented", "path": path},
        status_code=200,
    )
