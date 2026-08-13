# What we know about the modern Rec Room client
Notes from poking at the final 2026 Steam build after the June 1st shutdown.
The goal is to leave a clear record of what the client asks for and what it
expects back, so anyone trying to keep this thing alive doesn't have to
rediscover the obvious parts.

## The build in question
- Rec Room, Steam App ID 471710, the Against Gravity / Rec Room Inc. build.
- Unity 6000.0.27f1, which matters — it's the first Rec Room build on Unity 6
  and ships IL2CPP with the standard metadata file replaced by an encrypted
  blob, and the standard TLS stack replaced with a custom-validating one.
  Those two changes are what make the modern client effectively opaque to
  normal RE tooling.
- The version the client reports at the gate is `20260323.14` on platform 0.
- The RecNet environment is `prod`. Last known commit hash on the client side
  is `ac71d7fd3efa50f770de51e348391456ea30c681`.

The original shutdown announcement is at `blog.recroom.com/posts/schools-out-for-rec-room`
if you want the date and the corporate framing.

## Hosts the client talks to at launch
There are two that show up in DNS, SNI, and traffic captures the moment the
game starts:

| Host | What it's for | Backend |
| --- | --- | --- |
| `api.rec.net` | The version gate, game config, the nameserver bootstrap, and basically every other HTTP service the client calls. | Cloudflare, resolves to `104.18.8.90` / `104.18.9.90`. |
| `cdn.rec.net` | Static content. config payloads, assets, the big stuff. | Azure Front Door (`recnet-cdn-…a03.azurefd.net` → `mr-a03.tm-azurefd.net`), resolves to `150.171.109.51` / `150.171.109.53`. |

There are also a lot of sub-RecNet hosts the client knows about and will call
once the bootstrap has handed it a service map; see below. None of those
have been observed in real traffic on this build, because the client gets
stuck before it gets that far.

## The version gate
The first thing the client does after launch is call the version check. The
request is:

```
GET /api/versioncheck/v4?v=20260323.14&p=0
```

The response shape that exists in the running client and that the client
treats as "fine to continue" is:

```json
{
  "VersionStatus": 0,
  "UpdateNotificationStage": 0,
  "IsVersionIslanded": false,
  "IsCrossPlayDisabled": false
}
```

The shape that exists in the running client and that the client throws
`RecNet.VersionCheck.RecNetVersionUpdateRequiredException` for is:

```json
{
  "VersionStatus": 1,
  "UpdateNotificationStage": 0,
  "IsVersionIslanded": true,
  "IsCrossPlayDisabled": true
}
```

Both shapes have been observed in memory. The first is what the client wants
to see. The second is what the official server is currently returning
(which is why nobody can log in anymore. There's no official server to
return either one).

There's also a v3 endpoint, `/api/versioncheck/v3`, that returns
`{"ValidVersion": true}` for older clients, and an islanded-versions list
endpoint, `/api/versioncheck/islandedversions`, that returns `[]` for an
islanded island of one. The reference server in `server/` answers all three.

## The service map
The client receives a service map from a nameserver-style endpoint after the
version check passes. The full map recovered from the running process has 33
entries:

```json
{
  "Auth": "https://auth.rec.net",
  "BugReporting": "https://bugreporting.rec.net",
  "Cards": "https://cards.rec.net",
  "CDN": "https://cdn.rec.net",
  "Chat": "https://chat.rec.net",
  "Clubs": "https://clubs.rec.net",
  "CMS": "https://cms.rec.net",
  "Commerce": "https://commerce.rec.net",
  "Data": "https://data.rec.net",
  "DataCollection": "https://datacollection.rec.net",
  "Discovery": "https://discovery.rec.net",
  "Econ": "https://econ.rec.net",
  "GameLogs": "https://gamelogs.rec.net",
  "Geo": "https://geo.rec.net",
  "Images": "https://img.rec.net",
  "Leaderboard": "https://leaderboard.rec.net",
  "Link": "https://link.rec.net",
  "Lists": "https://lists.rec.net",
  "Matchmaking": "https://match.rec.net",
  "Moderation": "https://moderation.rec.net",
  "Notifications": "https://notify.rec.net",
  "PlatformNotifications": "https://platformnotifications.rec.net",
  "PlayerSettings": "https://playersettings.rec.net",
  "RoomComments": "https://roomcomments.rec.net",
  "RoomieIntegrations": "https://roomieintegrations.rec.net",
  "Rooms": "https://rooms.rec.net",
  "Storage": "https://storage.rec.net",
  "Strings": "https://strings.rec.net",
  "StringsCDN": "https://strings-cdn.rec.net",
  "Studio": "https://studio.rec.net",
  "Thorn": "https://thorn.rec.net",
  "Videos": "https://videos.rec.net",
  "WWW": "https://rec.net"
}
```

`RecNetStatus` exists as a key but its value is null in the current map.
The nameserver endpoint itself is one of `/`, `/2`, or `/services` — old
clients hit `/` or `/2`, modern ones seem to use the same convention.

## What the client asks for (HTTP request lines)
These are the request lines recovered from a running client. They cover
launch-time and early-session traffic only; everything past the version gate
is currently dark because the client never gets there.

```
GET /api/gameconfigs/v1/all
GET /api/versioncheck/islandedversions
GET /api/versioncheck/v4?v=20260323.14&p=0
GET /config/1b057e6e-979d-4f30-8856-a386f77c90da
POST /data/events
POST /api/v2/projects/7e820bcb-e196-459d-8e62-0ef049a6c5e6/reports
POST /v1/events
```

The two POSTs to the third-party-looking paths (`/api/v2/projects/.../reports`
and `/v1/events`) and `/data/events` seem like telemetry uploads, not gameplay
calls. The first four GETs are the launch bootstrap.

The `/config/{guid}` request is a per-build config blob. The GUID
`1b057e6e-979d-4f30-8856-a386f77c90da` is the one this build asks for; it
will differ for other builds.

## What's in the way
The modern client has three protections that together block every common RE
approach. Anyone trying to take this further is going to run into all three.

**Custom TLS validation.** The client doesn't trust the system CA store and
doesn't trust the cert the server presents unless it matches something hard
coded. This kills standard MITM. Every variation of proxy + system-CA-trust
that has been tried on this build fails at the TLS handshake, which means
the HTTP layer is invisible and only the SNI / DNS layer is observable.

**Encrypted IL2CPP metadata.** The on-disk `global-metadata.dat` is a dummy
file of ~50MB of `0x52` bytes (which I find a hilarious waste of space.)
`Il2CppDumper` against the real on-disk file produces nothing useful. 
The metadata does exist in memory, but Wine's anonymous
`/memfd:wine-mapping` regions mean the standard memory-dump-then-dump
workflow can't find the magic bytes. The result is that no function, class,
or field name is recoverable from the client through normal means. Though,
this is because I'm going through wine since I personally work with Linux.
Don't know how this would play under native Windows. Maybe worth a shot.

**Frida on Proton doesn't work.** The two main runtime-instrumentation paths
that would let you hook the version check or the auth flow both fail at
attach. That blocks the "I know the function, I can just patch its return
value" approach. Though, again, this is because I'm going through Linux
compatibility layers. Worth a try on Windows.

Together these are why the only thing this repo has actually gotten the
client to do is hit a local server for the version check, and only by
patching a single 18-byte pointer in the live process — a fix that breaks
the moment the client re-resolves the nameserver.

## What someone else will need to bring
A private server for the modern build needs three things to happen, in
roughly this order:

1. **Unpin TLS** so traffic can be captured. Without this, every endpoint
   past the version gate is undocumented. This is the highest-leverage
   single thing. Solving it would be huge; You could dump the full protocol in a
   single launch and the rest of the work becomes "build a server that
   speaks what the client was just observed asking for."
2. **Decrypt or otherwise recover the IL2CPP metadata** so the client can
   actually be read. The handful of strings (function names like
   `VersionCheck.VerifyGameVersion`, `get_RequiresUpdate`,
   `RecNetVersionCheckAccess.VersionCheck`) that show up as plaintext are
   useful breadcrumbs but don't substitute for a real type graph.
3. **Get frida working** so the runtime can be
   instrumented.

The community's existing private servers sidestep all three of these
by targeting old client builds where these protections weren't in
place.
That's all for now.
