import os
import secrets

DATABASE_URL = os.environ.get("OPENREC_DATABASE_URL", "sqlite:///./neorec.db")
SECRET_KEY = os.environ.get("OPENREC_SECRET_KEY", "").strip() or secrets.token_urlsafe(48)
PUBLIC_BASE_URL = os.environ.get("OPENREC_PUBLIC_BASE_URL", "http://127.0.0.1:8081")
PHOTON_APP_ID = os.environ.get("OPENREC_PHOTON_APP_ID", "").strip()
PHOTON_REGION = os.environ.get("OPENREC_PHOTON_REGION", "us")
ACCESS_TOKEN_EXPIRE_MINUTES = 525600
ADMIN_EMAIL = os.environ.get("OPENREC_ADMIN_EMAIL", "").strip().casefold()
FIREBASE_WEB_API_KEY = os.environ.get(
    "OPENREC_FIREBASE_WEB_API_KEY",
    "",
).strip()
FLUX_ALLOWED_ORIGINS = [
    origin.strip()
    for origin in os.environ.get(
        "OPENREC_FLUX_ALLOWED_ORIGINS",
        "http://localhost:3000,http://127.0.0.1:3000",
    ).split(",")
    if origin.strip()
]
