# Flux Rec local service

The FastAPI service provides the legacy client endpoints implemented by this project. From the repository root:

```powershell
py -3 -m venv .venv
.\.venv\Scripts\python.exe -m pip install -r server\requirements.txt
.\.venv\Scripts\python.exe -m uvicorn server.main:app --host 127.0.0.1 --port 8081
```

Copy `.env.example` to `.env` and let `Start-FluxRec.ps1` load it, or set the variables in the process environment. Keep the SQLite database and secret values outside Git.

