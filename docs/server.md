# Standalone server

`Marvel.Server` runs the same authoritative engine used by the embedded Godot
client. It serves one length-prefixed request and response per TCP connection.
Use it for local development or on a trusted private network.

## Support boundary

The Linux container below is the supported standalone deployment and is built,
played through, and stopped cleanly by CI. Direct
`dotnet run` launch on macOS, Windows and Linux is the development path; live
sessions are in-memory and are not a production persistence service.

## Start a local server

Run this command from the repository root:

```bash
dotnet run --project src/Marvel.Server/Marvel.Server.csproj -- \
  --listen 127.0.0.1 \
  --port 41923 \
  --data-root . \
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
  --visibility cooperative
```

To listen on a private network, replace `127.0.0.1` with the server's private
IP address. Set `MARVEL_ENGINE_ENDPOINT` to that same address on each client.
The operator must also allow the selected TCP port through the host firewall.

Do not expose this service to the Internet. The protocol is not encrypted, and
session capabilities and seat invitations are bearer credentials. Anyone who
obtains one can use its authority until that session closes or the server stops.

## Choose server options

The process accepts these options:

| Option | Default | Meaning |
|---|---:|---|
| `--listen IP` | `127.0.0.1` | Local IP address on which the server listens. Host names are not accepted. |
| `--port NUMBER` | `41923` | TCP port from 1 to 65535. |
| `--data-root PATH` | Current directory | Repository or published-data root containing the required datasets. |
| `--visibility cooperative` | `cooperative` | Shows the whole cooperative table. Client viewer claims cannot hide or reveal seats. |
| `--visibility restricted --seat NUMBER` | None | Binds the opening session to one non-negative seat number. |

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
startup failure prints one diagnostic to standard error and exits with code 2.

## Run the container

Build from the repository root because the Docker build copies the solution and
the required datasets:

```bash
docker build --file src/Marvel.Server/Dockerfile --tag marvel-server .
docker run --rm --publish 127.0.0.1:41923:41923 marvel-server
```

The image publishes a framework-dependent .NET 8 application. It contains the
cards, setup and ability datasets needed at runtime. Its entry point listens on
`0.0.0.0:41923` inside the container and uses `/app` as the data root. The host
mapping above keeps the service reachable only through host loopback.

Append visibility options to the `docker run` command when needed:

```bash
docker run --rm --publish 127.0.0.1:41923:41923 marvel-server \
  --visibility restricted --seat 0
```

The container has no persistent game volume. Mounting a volume does not add
session persistence because live sessions exist only in process memory.

## Stop and restart safely

Press `Ctrl+C` to stop the process. On macOS and Linux, `SIGTERM` follows the
same path, including the signal sent by `docker stop`. The server stops
accepting connections and allows the request it is currently serving to finish.
A client that does not finish its frame cannot hold the server indefinitely;
accepted connections use a 30-second receive and send timeout.

All games, capabilities and unused invitations live only in memory. Stopping or
restarting the process removes them, so connected clients must open a new game.

Once transmission of a mutation begins, a client-side write or response failure
cannot prove that the server did not apply it. The client therefore never
repeats an ambiguous decision; it synchronizes with the existing capability.
Every prompt and snapshot also carries a host revision. A resolve echoes the
revision it answers, and the server rejects an older revision as
`stale_decision` before gameplay code runs.
