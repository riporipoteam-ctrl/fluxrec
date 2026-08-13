from mitmproxy import http

TARGET_HOSTS = {"api.rec.net", "cdn.rec.net"}


def request(flow: http.HTTPFlow) -> None:
    if flow.request.host not in TARGET_HOSTS:
        return

    print(f">>> {flow.request.method} {flow.request.pretty_url}")
    for name, value in flow.request.headers.items():
        if name.lower() in {"authorization", "cookie"}:
            value = "<redacted>"
        print(f">>>   {name}: {value}")
    if flow.request.content:
        print(flow.request.get_text(strict=False)[:4000])


def response(flow: http.HTTPFlow) -> None:
    if flow.request.host not in TARGET_HOSTS:
        return

    print(
        f"<<< {flow.response.status_code} {flow.request.method} {flow.request.pretty_url}"
    )
    for name, value in flow.response.headers.items():
        if name.lower() in {"set-cookie"}:
            value = "<redacted>"
        print(f"<<<   {name}: {value}")
    if flow.response.content:
        print(flow.response.get_text(strict=False)[:4000])
