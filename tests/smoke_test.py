import json
import os
from pathlib import Path
import socket
import subprocess
import sys
import tempfile
import time
import urllib.parse
import urllib.request


REPOSITORY_ROOT = Path(__file__).resolve().parents[1]


def free_port() -> int:
    with socket.socket() as listener:
        listener.bind(("127.0.0.1", 0))
        return int(listener.getsockname()[1])


def request_json(url: str, data: bytes | None = None, headers: dict | None = None):
    request = urllib.request.Request(url, data=data, headers=headers or {})
    with urllib.request.urlopen(request, timeout=5) as response:
        assert response.status == 200
        return json.load(response)


def main() -> None:
    port = free_port()
    base_url = f"http://127.0.0.1:{port}"
    # Windows can keep SQLite's file handle alive for a moment after the child
    # exits. The test has already isolated the database, so delayed OS cleanup
    # must not turn a successful API run into a false negative.
    with tempfile.TemporaryDirectory(
        prefix="fluxrec-smoke-", ignore_cleanup_errors=True
    ) as temporary_directory:
        database_path = Path(temporary_directory, "smoke.db").as_posix()
        environment = os.environ.copy()
        environment.update(
            {
                "OPENREC_DATABASE_URL": f"sqlite:///{database_path}",
                "OPENREC_SECRET_KEY": "smoke-test-secret-not-for-production",
                "OPENREC_PUBLIC_BASE_URL": base_url,
                "OPENREC_REQUEST_LOG": "",
            }
        )
        server = subprocess.Popen(
            [
                sys.executable,
                "-m",
                "uvicorn",
                "server.main:app",
                "--host",
                "127.0.0.1",
                "--port",
                str(port),
                "--log-level",
                "warning",
            ],
            cwd=REPOSITORY_ROOT,
            env=environment,
        )

        try:
            for _ in range(75):
                try:
                    request_json(f"{base_url}/api/versioncheck/v4")
                    break
                except Exception:
                    if server.poll() is not None:
                        raise RuntimeError(f"server exited with code {server.returncode}")
                    time.sleep(0.2)
            else:
                raise RuntimeError("server did not become ready")

            services = request_json(f"{base_url}/services")
            assert services["Auth"] == "https://auth.rec.net"
            assert services["API"] == "https://api.rec.net"

            username = f"smoke_{int(time.time())}"
            signup_payload = json.dumps(
                {
                    "username": username,
                    "password": "test-password",
                    "displayName": "Smoke Test",
                }
            ).encode()
            signup = request_json(
                f"{base_url}/signup",
                signup_payload,
                {"Content-Type": "application/json"},
            )
            token = signup["access_token"]

            login_payload = urllib.parse.urlencode(
                {"username": username, "password": "test-password"}
            ).encode()
            login = request_json(
                f"{base_url}/connect/token",
                login_payload,
                {"Content-Type": "application/x-www-form-urlencoded"},
            )
            assert login["access_token"]

            settings = request_json(
                f"{base_url}/playersettings",
                headers={"Authorization": f"Bearer {token}"},
            )
            assert settings == []
            print("Flux Rec backend smoke test passed")
        finally:
            server.terminate()
            try:
                server.wait(timeout=10)
            except subprocess.TimeoutExpired:
                server.kill()
                server.wait(timeout=5)


if __name__ == "__main__":
    main()
