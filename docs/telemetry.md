# Operational telemetry

Telemetry is an optional consumer of the structured operational record. The
default exporter is a no-op, and the server performs no remote telemetry I/O
unless an operator supplies `--telemetry-endpoint URL`. The Godot client uses
the same exporter only when `MARVEL_TELEMETRY_ENDPOINT` is explicitly set to an
allowed URL.

Each accepted operational record produces one schema-1 envelope containing a
bounded list of metric observations and one measured span. Transport, replay,
persistence and host completion records share a trace correlation. The metric
names are:

- `marvel.request.outcomes` and `marvel.request.latency_ms`;
- `marvel.sessions.active` and `marvel.sessions.reconnects`;
- `marvel.saves.committed` and `marvel.replay.divergences`;
- `marvel.undo.refusals`; and
- `marvel.trace_rewrites.accepted`.

Dimensions are limited to stable process, event, operation, disposition and
error-code vocabularies. Game labels, request labels, capabilities, invitations,
card ids, card faces, payments, save bodies and exception messages are never
dimensions. Trace ids are one-way correlations derived from the already
pseudonymous request or game correlation. Save and replay work are boolean span
attributes, not payloads.

`marvel.sessions.active` is a process-local gauge, not a cumulative counter.
Opening or restoring a session publishes the current loaded count; retiring one
publishes the reduced count. A restarted server therefore replaces its prior
value as it restores saves instead of adding another lifetime delta.

The HTTP exporter posts each envelope once, requires HTTPS except for loopback
development, has a two-second timeout and never retries. It runs behind its own
process-wide 1,024-envelope bound, separate from the structured-log dispatcher.
A slow or failed endpoint can delay or drop later telemetry, but it cannot delay
local diagnostics or fail, retry or reorder gameplay.
Normal server shutdown gives the structured-log and telemetry queues one shared,
three-second drain budget. Anything still pending after that bound is abandoned.

## Privacy and retention policy

This project does not persist telemetry locally and does not prescribe a remote
retention period. The operator controls the receiving system and must configure
access, deletion and retention before enabling export. Retain operational
telemetry only as long as needed to diagnose service health and product flows;
30 days is the recommended default. Do not join telemetry correlations to bearer
credentials or external player identities.
