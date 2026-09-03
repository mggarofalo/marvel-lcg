# Standalone server

`Marvel.Server` runs the same authoritative engine used by the embedded Godot
client. It serves one length-prefixed request and response per TCP connection.
Use it for local development or on a trusted private network.

## Support boundary

The Linux container below is the supported standalone deployment and is built,
played through, and stopped cleanly by CI. Direct
`dotnet run` launch on macOS, Windows and Linux is the development path. The
server commits every accepted decision to its configured save root before it
acknowledges the command.

## Start a local server

Run this command from the repository root:

```bash
dotnet run --project src/Marvel.Server/Marvel.Server.csproj -- \
  --listen 127.0.0.1 \
  --port 41923 \
  --data-root . \
  --save-root "$HOME/Library/Application Support/MarvelLCG/sessions" \
  --visibility cooperative
```

Then launch the Godot client with the matching endpoint:

```bash
MARVEL_ENGINE_ENDPOINT=tcp://127.0.0.1:41923 "$GODOT_BIN" --path src/Marvel.Godot
```

The equivalent Windows PowerShell development launch is:

```powershell
dotnet run --project src/Marvel.Server/Marvel.Server.csproj -- `
  --listen 127.0.0.1 `
  --port 41923 `
  --data-root . `
  --save-root "$env:LOCALAPPDATA\MarvelLCG\sessions" `
  --visibility cooperative
```

To listen on a private network, replace `127.0.0.1` with the server's private
IP address. Set `MARVEL_ENGINE_ENDPOINT` to that same address on each client.
The operator must also allow the selected TCP port through the host firewall.

Do not expose this service to the Internet. The protocol is not encrypted, and
session capabilities and seat invitations are bearer credentials. Anyone who
obtains one can use its authority until that credential or session is closed.

## Choose server options

The process accepts these options:

| Option | Default | Meaning |
|---|---:|---|
| `--listen IP` | `127.0.0.1` | Local IP address on which the server listens. Host names are not accepted. |
| `--port NUMBER` | `41923` | TCP port from 1 to 65535. |
| `--data-root PATH` | Current directory | Repository or published-data root containing the required datasets. |
| `--save-root PATH` | OS local application data under `MarvelLCG/sessions` | Private directory containing committed session generations and credential verifiers. |
| `--visibility cooperative` | `cooperative` | Shows the whole cooperative table. Client viewer claims cannot hide or reveal seats. |
| `--visibility restricted --seat NUMBER` | None | Binds the opening session to one non-negative seat number. |
| `--telemetry-endpoint URL` | Disabled | Posts redacted metric and trace envelopes to explicit HTTPS, or loopback HTTP for development. |

For example, this server authorizes the opening client as seat 0:

```bash
dotnet run --project src/Marvel.Server/Marvel.Server.csproj -- \
  --listen 127.0.0.1 \
  --port 41923 \
  --data-root . \
  --visibility restricted \
  --seat 0
```

Opening a multiplayer game under this policy returns one-time invitations for
the other seats. Redeeming an invitation creates a separate capability that can
see and answer only for its assigned seat. The opening capability remains bound
to seat 0.

In the Godot client, choose Start, enter this server's endpoint and select a
second hero. After the game opens, use Copy invitation once and send the copied
secret to the second player through a trusted channel. That player chooses Join
and enters the same endpoint and game label. The client masks and clears the
invitation before redeeming it. It never shows the secret in status or error
text.

The server rejects unknown options, missing values, invalid IP addresses,
out-of-range ports and invalid visibility combinations before listening. It
also loads and validates the required datasets before opening the socket. A
startup failure prints one bounded structured record to standard error and
exits with code 2. The record never includes the rejected path or exception
message.

## Read operational logs

The standalone server and desktop composition write the same JSON-lines record
shape to standard error. Stable `event_id` values identify request completion,
session restore, listener readiness, startup failure and client transport
completion. Named fields correlate the process, timestamp, duration,
pseudonymous request and game identifiers, operation, revision, authorized seat, disposition, save commit,
replay verification and bounded error code.

The schema has no field for capabilities, invitations, cards, payments, save
bodies or exception text. Request and game correlations are one-way 32-character
digests; other string fields are bounded to 256 characters, and unknown
operation names are not copied into records. A process-wide logging dispatcher
is a bounded, asynchronous observer: failure or delay while writing a record
cannot fail, retry, or delay a game command. Wall-clock timestamps, process ids
and durations exist only in these operational records and never enter a save,
replay, RNG stream or state digest.

Telemetry is off by default. Supplying `--telemetry-endpoint` adds a no-retry,
two-second HTTP exporter behind the same bounded observer dispatcher. See
[telemetry.md](telemetry.md) for its schema, privacy boundary and retention
policy.

## Run the container

Preview and stable releases publish the Linux/amd64 image at
`ghcr.io/mggarofalo/marvel-server`. Install the immutable digest recorded in the
GitHub release rather than treating its readable version tag as an installation
identity:

```bash
image=ghcr.io/mggarofalo/marvel-server@sha256:RELEASE_DIGEST
docker pull "$image"
docker run --detach --name marvel-server \
  --stop-timeout 40 \
  --publish 127.0.0.1:41923:41923 \
  --volume marvel-sessions:/var/lib/marvel/sessions \
  --read-only --cap-drop ALL --security-opt no-new-privileges \
  --memory 512m --cpus 1 --pids-limit 128 \
  "$image"
```

The image runs as the .NET base image's unprivileged application user. Its
application and dataset layers are read-only; `/var/lib/marvel/sessions` is the
only persistent write location. It contains the cards, setup and ability
datasets needed at runtime, listens on `0.0.0.0:41923`, and uses `/app` as the
data root. The host mapping above keeps the service reachable only through host
loopback. The resource limits are the supported starting point for one small
table; monitor the container and raise them deliberately for concurrent tables.

After verifying the released digest, the checked-in Compose definition starts
that same hardened service with an explicit bridge network and named save
volume. The image variable has only a deliberately non-runnable sentinel
default: starting the service requires an operator to name the immutable
artifact that was verified, while later `docker compose down` remains usable
without reconstructing that environment variable.

```bash
MARVEL_SERVER_IMAGE=ghcr.io/mggarofalo/marvel-server@sha256:RELEASE_DIGEST \
  docker compose up --detach
```

`MARVEL_SERVER_BIND` and `MARVEL_SERVER_PORT` may override the default
`127.0.0.1:41923` host endpoint. `docker compose down` removes the container
and `marvel-server` network but retains the explicitly named `marvel-sessions`
volume. The service container is explicitly named `marvel-server`, so the
backup and troubleshooting commands below address the same resources in both
launch forms. Never add `--volumes` unless the saved sessions have been backed
up and are deliberately being destroyed.

The release includes a Sigstore bundle named
`MarvelServer-VERSION-linux-amd64.sigstore.json`. Verify the image before first
use, substituting the released version, digest, repository owner and tag:

```bash
cosign verify \
  --bundle MarvelServer-VERSION-linux-amd64.sigstore.json \
  --certificate-identity \
    'https://github.com/OWNER/REPOSITORY/.github/workflows/release-desktop.yml@refs/tags/vVERSION' \
  --certificate-oidc-issuer 'https://token.actions.githubusercontent.com' \
  ghcr.io/OWNER/marvel-server@sha256:RELEASE_DIGEST
```

The matching `.digest` and `.provenance.json` release files record the exact
image, commit, protocol, replay contracts and three runtime dataset hashes. To
inspect a running build without opening or changing a game:

```bash
docker exec marvel-server dotnet Marvel.Server.dll --version
docker inspect --format '{{if .State.Health}}{{.State.Health.Status}}{{end}}' marvel-server
```

The Docker health check makes a real setup/version request over loopback. A
healthy result therefore means the process is listening, the runtime datasets
loaded, and the server returned its expected protocol and product identity. It
does not assert that any particular saved session is compatible: incompatible
sessions are isolated while healthy sessions remain available.

For a local development image, build from the repository root because the
Docker build copies the solution and required datasets:

```bash
docker build --file src/Marvel.Server/Dockerfile --tag marvel-server-dev .
```

Append visibility options to the `docker run` command when needed:

```bash
docker run --rm --publish 127.0.0.1:41923:41923 marvel-server-dev \
  --visibility restricted --seat 0
```

Do not mount any application or dataset path writable. Logs and diagnostics are
JSON lines on standard error, so collect them with the container runtime rather
than granting another filesystem write location. Capabilities and invitations
remain client-held bearer secrets; do not put them in container environment
variables, labels, health checks, backups of logs, or diagnostic bundles.

## Back up and restore the save volume

Backups are consistent only while the server is stopped. `docker stop` sends
`SIGTERM`; the server stops accepting work, lets the current request finish,
and exits after its atomic save generation is durable. The supported run command
sets a 40-second stop timeout. Shutdown closes an incomplete client read or
response immediately; a decision already dispatched to the sequential engine
still reaches its synchronous atomic commit before exit, and the final log
flush has a three-second budget. Confirm exit code zero before archiving the
named volume:

```bash
docker stop marvel-server
test "$(docker inspect --format '{{.State.ExitCode}}' marvel-server)" = 0
docker run --rm \
  --volume marvel-sessions:/source:ro \
  --volume "$PWD":/backup \
  alpine:3.23.3 tar -C /source -czf /backup/marvel-sessions.tgz .
```

Pin and verify the helper image in production by digest under the operator's
own supply-chain policy. Protect the archive as authentication data: saves hold
hashed capability and invitation verifiers even though plaintext credentials
are never persisted.

Restore into an empty volume while no server uses it, then start the exact
application digest that the backup is known to support:

```bash
docker volume create marvel-sessions-restored
docker run --rm \
  --volume marvel-sessions-restored:/target \
  --volume "$PWD":/backup:ro \
  alpine:3.23.3 tar -C /target -xzf /backup/marvel-sessions.tgz
```

Never overlay a backup onto a populated volume. Keep the old volume until the
restored server is healthy and its expected tables have synchronized.

Before an upgrade, stop the server and preserve both the current image digest
and a complete volume backup. A newer version replay-verifies each session and
atomically migrates any supported older schema. A corrupt, divergent,
dataset-incompatible, or newer-last-writer session is quarantined and reported
with a bounded `session.restore.failed` error code; it does not prevent other
sessions or the listener from becoming ready. Recover that table from a known
good backup or with a future runtime that explicitly supports its identity.

Do not point an older runtime at a volume written by a newer product version.
The last writer's SemVer is stamped on every gameplay, history, lifecycle and
migration commit, and the older runtime quarantines that session as
`unsupported_downgrade`. Rolling back requires restoring the matching
pre-upgrade backup into a fresh volume as well as restoring the prior image.

## Stop and restart safely

Press `Ctrl+C` to stop the process. On macOS and Linux, `SIGTERM` follows the
same path, including the signal sent by `docker stop`. The server stops
accepting connections and allows the request it is currently serving to finish.
A client that does not finish its frame cannot hold the server indefinitely;
accepted connections use a 30-second receive and send timeout.

Active games, hashed capability verifiers and unused invitation verifiers are
restored from the last committed generation. Plaintext bearer credentials are
never written; clients retain their existing capability and synchronize after
the server restarts. A corrupt, incompatible or divergent generation prevents
only that table from being published rather than guessing at a plausible board.

Once transmission of a mutation begins, a client-side write or response failure
cannot prove that the server did not apply it. The client therefore never
repeats an ambiguous decision; it synchronizes with the existing capability.
Every prompt and snapshot also carries a host revision. A resolve echoes the
revision it answers, and the server rejects an older revision as
`stale_decision` before gameplay code runs.

## Troubleshoot a hosted table

Start with read-only evidence. Record the image digest, the output of
`Marvel.Server.dll --version`, container health, and the relevant JSON lines
before changing a volume. The client status and server record distinguish the
supported recovery paths:

| Evidence | Meaning | Safe next action |
|---|---|---|
| `SERVICE UNAVAILABLE` | The endpoint could not be reached; this does not prove whether a sent mutation committed. | Check the bind address, published port, firewall and container health. Restart the service if needed, then use Synchronize. Never repeat a mutation marked unconfirmed. |
| `VERSION MISMATCH` or `unsupported_version` | Client and server do not share the wire protocol. | Compare the client identity toolbar with `Marvel.Server.dll --version`; update one side to the intended release. |
| `SESSION UNAVAILABLE` or `session_not_found` | The server established only that the held capability or invitation is not usable; it does not disclose whether it expired, was already used, or was never valid. | Check the game label. For an invitation, ask the host for a new one. For a previously working session, inspect restore records and recover a known-good volume backup if the table should exist. |
| `STORAGE FAILURE`, `save_failed`, or `session.persistence.completed` with `rejected` | The requested state was not durably committed. | Stop new play, verify volume space, ownership and read/write status, and preserve logs. Synchronize the last authoritative revision; do not repair save files by hand. |
| `session.restore.completed` | A saved session replay-verified and was published after startup. | Reconnect with the existing capability and Synchronize. |
| `session.restore.failed` | One saved table was quarantined; other tables may still be healthy. | Match its pseudonymous game correlation and bounded error code, preserve the volume, then restore a known-good backup or use a runtime that supports the recorded identity. |
| `server.listener.stopped` | The listener completed an intentional graceful shutdown and flushed its bounded log queue. | Archive the stopped save volume only after also confirming exit code zero. Absence of this record means shutdown was not confirmed graceful. |

Useful collection commands are read-only:

```bash
docker inspect marvel-server > marvel-server.inspect.json
docker logs --timestamps marvel-server > marvel-server.log.jsonl 2>&1
docker exec marvel-server dotnet Marvel.Server.dll --version
docker volume inspect marvel-sessions
```

Do not publish the collected files without review. The application schema
excludes bearer credentials and concealed card state, but container metadata or
surrounding platform logs may contain operator-added values. Standard error is
an ephemeral runtime stream unless the container platform retains it; durable
rotation, incident export and failure-drill requirements are tracked separately
from this deployment workflow.

If a response was lost, correlate `transport.exchange.completed`,
`server.request.completed`, and `client.reconnect.completed` by their
pseudonymous request/game identifiers, operation, revision, disposition and
`save_committed`. A server `accepted` record with `save_committed:true` proves
the mutation reached durable state even if the client never received its
response. A rejected record proves the engine did not accept that request. If
the server record is absent, treat the outcome as uncertain and recover only by
reading the authoritative session.
