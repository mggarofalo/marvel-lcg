# Release identity, signing and compatibility

This document is the release contract for the desktop client and persistent
multiplayer server. It defines the identities a build carries, which versions
may communicate with or restore one another, and where signing authority lives.
Artifact production and installation implement this policy; they do not get to
invent a second compatibility policy.

These are product choices. The tabletop rules say nothing about application
versions, protocols, save schemas, release channels, signatures or upgrades.

## Release identities

One release of this repository has one product version shared by its macOS
desktop artifact, Windows desktop artifact and Linux server artifact. Product
versions use SemVer 2.0.0:

```text
MAJOR.MINOR.PATCH[-PRERELEASE][+BUILD]
```

- A stable release is tagged `vMAJOR.MINOR.PATCH` and displays
  `MAJOR.MINOR.PATCH`.
- A preview is tagged `vMAJOR.MINOR.PATCH-preview.N` and displays the same
  version without the leading `v`.
- An untagged build is a developer build. It displays
  `MAJOR.MINOR.PATCH-dev.N+COMMIT`, is unsigned, and is never described as a
  release artifact.
- `N` is a monotonically increasing build number in its channel. `COMMIT` is
  the complete lowercase Git commit id in artifact metadata; a UI may show an
  unambiguous abbreviation.

The first delivery line is `0.1.0`. Before its first protected tag, developer
builds use `0.1.0-dev.N+COMMIT`. A major increment may remove an operator or
player workflow that the prior major version supported. A minor increment adds
a backward-compatible feature. A patch increment fixes existing behavior. The
independent protocol and save checks below still decide interoperability.

Build metadata identifies an input but does not change SemVer precedence. A
release is immutable: rebuilding changed source or datasets requires a new
product version, even when the intended behavior is equivalent.

Product version answers “which shipped build is this?” It does not by itself
decide whether two builds can communicate or replay a save. The following
independent identities do:

| Identity | Current value | Changes when |
|---|---:|---|
| Engine protocol | `11` | A request, response, affordance, event or descriptor change is not understood by the prior endpoint. |
| Session schema | `2` | The strict persisted JSON shape changes. |
| Engine replay contract | `engine-replay-v1` | The same setup and decision trace may resolve differently. |
| RNG contract | `mt19937-iso-cxx` | The seeded random stream changes. |
| State digest | `state-digest-v2` | The canonical hidden-state serialization changes. |
| Runtime datasets | Three SHA-256 values | Any byte in `cards.json`, `setup.json` or `abilities.json` changes. |

The product version belongs in desktop and server metadata. The protocol
version belongs on every request and response. Save schema and replay identities
belong in the save compatibility record. They must not enter gameplay state,
the RNG stream or the state digest merely to support display or diagnostics.

### Artifact names and embedded versions

Release filenames use the product version without the leading tag `v`:

```text
MarvelChampions-MAJOR.MINOR.PATCH[-PRERELEASE]-macos-adhoc.zip
MarvelChampions-MAJOR.MINOR.PATCH[-PRERELEASE]-windows-x64-community.msix
MarvelChampions-MAJOR.MINOR.PATCH[-PRERELEASE]-windows-x64-community.cer
MarvelChampions-MAJOR.MINOR.PATCH[-PRERELEASE]-windows-x64-portable-unsigned.zip
marvel-server:MAJOR.MINOR.PATCH[-PRERELEASE]
```

The server name is an OCI image tag. The release record also pins its immutable
image digest, since a mutable tag is not an installation identity.

Every .NET assembly uses `MAJOR.MINOR.PATCH.0` for `AssemblyVersion` and
`FileVersion`. Its `InformationalVersion` contains the complete product version
and commit. Godot, the desktop file properties, the server `--version` output
and OCI image labels expose that same informational version. The release
pipeline rejects an artifact when any embedded value disagrees with the tag or
manifest.

Windows preview and stable packages share one MSIX package-family name and
publisher identity so a preview can update to its stable release. MSIX requires
a four-part numeric version that increases for an update. The package manifest
maps SemVer to `MAJOR+1.MINOR.PATCH.REVISION`:

- preview `N` uses revision `N`, from `1` through `65534`;
- stable uses revision `65535`; and
- developer builds do not use the release MSIX package identity.

For example, `0.1.0-preview.7` maps to `1.1.0.7`, and `0.1.0` maps to
`1.1.0.65535`. A later `0.1.1-preview.1` maps to `1.1.1.1` and is still newer.
The release input rejects a SemVer component outside the MSIX numeric range or
a product major above `65534`. The signed package displays the SemVer product
version; its numeric identity exists only for Windows update ordering.

Saves record the complete SemVer product version of the runtime that last
committed them in `compatibility.application`. Every successful gameplay,
history, lifecycle or migration commit stamps the current version in the same
atomic generation. Loading without a commit does not rewrite it. This makes the
field a durable downgrade floor instead of the version that first created the
table. SemVer comparison ignores build metadata.

Saves from the earlier developer-only runtime contain a four-part assembly
version instead. They are not supported release inputs and remain quarantined
unless a later issue adds and verifies an explicit migration.

## Release channels

There are three channels:

| Channel | Intended use | Desktop trust | Upgrade promise |
|---|---|---|---|
| Developer | Local work and pull-request artifacts | Unsigned | None; use disposable data. |
| Preview | Release-candidate testing | Ad-hoc macOS and unsigned portable Windows; self-signed Windows MSIX | Forward upgrades within the same preview line only when the compatibility checks below pass. |
| Stable | Community installation | Ad-hoc macOS and unsigned portable Windows; self-signed Windows MSIX | Forward upgrades within the same major version when the compatibility checks below pass. |

Preview and stable artifacts are produced only from their exact protected Git
tag after the ordinary build, test, dataset, native-client and package gates
pass. A preview never silently becomes stable; promotion creates a stable tag
and a new release record from an explicitly identified commit.

Installing a lower SemVer product version is a downgrade, regardless of
channel. Downgrades are unsupported against an existing save volume. An
operator may install an older artifact only with an independently preserved
backup known to have been written by that artifact and only after removing the
newer runtime from service.

## Client and server compatibility

The client and server accept exactly one engine protocol version. There is no
range negotiation and no best-effort parsing:

1. Every request carries its protocol version.
2. A server rejects any value other than its own before gameplay code runs,
   using the bounded `unsupported_version` error.
3. A client rejects a response with any value other than its own as
   `unsupported_version` and does not interpret its prompt, events or snapshot.
4. A rejected mutation is not retried. If transmission made its result
   uncertain, the client follows the existing synchronization rule rather than
   guessing from a version error.

Consequently, mixed product versions are allowed only when both artifacts
declare the same protocol integer. Patch or minor releases may retain that
integer when the wire is unchanged. A new optional-looking union variant still
requires a protocol decision: an older peer cannot be assumed to understand it.

The setup/version discovery response exposes the server product version,
source commit, protocol version, replay identities, save schema and runtime
dataset identity before a game is opened. The desktop About/diagnostic surface
exposes the same identities for its own build. This policy defines their values
and comparison.

## Save and dataset compatibility

A save is server-owned. A desktop client never uploads, rewrites or migrates
one. The server evaluates compatibility before dealing or publishing a restored
game.

The application version in a save is a last-writer provenance and downgrade
gate, not an equality gate. A newer product version may read an older save when
every compatibility identity still matches. A runtime may restore a save only
when all of the following hold:

- its product version is not lower than the saved application version;
- the save schema is the current schema or a specifically implemented readable
  predecessor;
- the engine replay, RNG and state-digest identities are supported;
- the card, setup and ability dataset SHA-256 values match exactly; and
- complete deterministic replay verifies every recorded prompt, event, RNG
  count and state fingerprint.

Schema 2 is the current writer. Schema 1 is its single readable predecessor.
Reading schema 1 performs the implemented replay-verified, atomic migration and
commits schema 2 before publishing the session. “Same final board” is never
sufficient evidence for migration.

An unknown schema, replay identity, RNG identity, digest identity or dataset
hash is unsupported. The server quarantines that session and reports a bounded
compatibility failure; it does not skip records, substitute current data or
offer a partially restored table. A storage or migration failure leaves the
last committed generation authoritative.

A newer runtime may deliberately support another predecessor by shipping an
explicit parser, migration and replay tests. A release note must name the exact
source identity and destination identity. Merely retaining a decoder is not an
upgrade promise.

Before upgrading a server, the operator stops new work and makes a backup of
the complete save and protected-authentication volume. A successful upgrade
verifies and, when required, atomically migrates every active session before it
is published. If any session is unsupported or divergent, that session remains
quarantined and the operator restores the prior artifact together with its
matching pre-upgrade backup. New-format saves are never handed to an older
runtime as a downgrade strategy.

## Dataset identity

The runtime dataset identity is the ordered tuple of lowercase SHA-256 digests
already recorded by `SessionCompatibility`:

1. `datasets/cards/cards.json`;
2. `datasets/setup/setup.json`; and
3. `datasets/abilities/abilities.json`.

The release record publishes all three hashes. Desktop and server packages for
one product version must contain byte-identical runtime datasets. Broader
vendored research corpora are not runtime identity merely because they are in
the repository.

Changing a runtime dataset requires a new product version. Existing saves do
not become compatible because the new dataset looks equivalent; an explicit
replay-contract migration must prove that claim.

## Community desktop artifacts

The project publishes desktop builds without paid trust services. Each build
starts from a clean protected tag and pinned tools. The build embeds the release
manifest, normalizes reproducible inputs, and publishes a SHA-256 file beside
each artifact.

The macOS ZIP has a timestamp-free ad-hoc signature. The build replaces Godot's
inherited template signature and seals the complete app without claiming a
publisher identity. The app is not notarized, Developer ID signed, or accepted
by Gatekeeper without a user decision. The release page and installation guide
must say this before asking anyone to download or open it.

Windows publishes 3 related outputs:

- The unsigned MSIX is the reproducible package input and is useful for audit.
- The portable ZIP is unsigned and runs without adding a certificate trust.
- The community MSIX is signed by a release-specific self-signed certificate. The
  matching public `.cer` file is published beside it.

The Windows package family uses publisher `CN=Marvel Champions Community`.
Release automation creates a release-specific, non-exportable private key in
the temporary runner certificate store. It removes that key and its temporary
trust entry even when the job fails. The private key is never uploaded, cached,
logged, committed, or retained as a release credential.

The community MSIX has no trusted timestamp. Each release therefore gets a new
certificate, and Windows does not trust it by default. A user must verify the
artifact hashes, inspect the certificate subject and fingerprint, then choose
whether to add that exact public certificate to Trusted People. Removing the
package does not remove the certificate; the user must remove both.

The workflow verifies the package signature, publisher, lack of timestamp,
embedded payload hashes, and SemVer-to-MSIX mapping before publication. This
proves integrity after the user trusts the attached certificate. It does not
claim public identity or reputation.

SignTool must reject the release certificate as publicly untrusted. Automation
requires that exact chain verdict and rejects any other signature failure. It
separately verifies the package publisher, signature presence and payload
hashes. The runner removes its temporary Trusted People entry, then deletes the
private certificate with its backing key. It verifies that both entries are
gone.

No Apple membership, commercial certificate, paid timestamp, or paid signing
service is part of the supported release path. Adding commercial platform trust
would be separate work with separate authorization.

No private key, password, API key, provisioning credential, or signing token
belongs in the repository, build cache, artifact, save, log, or telemetry
record. The active `refs/tags/v*` ruleset prevents tag updates and deletion.
The workflow also checks `GITHUB_REF_PROTECTED` before it builds a release.

### Linux server image

The Linux release job signs the immutable, single-platform OCI image
digest. Build timestamps are rewritten from the tagged commit's source epoch;
invocation-specific attestations are not embedded in that image index, and the
compatibility provenance is a separate release record. It therefore does not
change the reproducible installation identity. The job uses keyless
Sigstore signing through its short-lived OpenID Connect identity.
GitHub grants the job a short-lived identity for the protected tag. The
repository stores no long-lived image-signing key.

The signature binds the digest to this repository, the release workflow and
the protected tag. The release record retains the verification bundle and its
transparency-log evidence beside the image digest. Installation verifies the
signature against the pinned issuer and workflow identity before it pulls or
runs the image. Verifying only the mutable image tag or an unauthenticated
checksum file is not sufficient.

Signing adds a separate OCI signature and does not change the unsigned image
digest. This preserves the link from the signed release record to the
reproducible unsigned server input.

## Responsibility and verification

Ordinary CI, on every pull request and supported runner, owns deterministic
compilation, managed tests, dependency walls, dataset checks, native Godot
smoke tests, unsigned package construction and ephemeral Windows self-signing.
It has no retained signing credential or public-trust authority.

Release automation owns tag validation, clean-source verification, version
stamping, artifact hashing, provenance and publication. macOS verifies the
ad-hoc application structure and reproducibility. Windows verifies the
self-signed package and deletes its temporary private key. The Linux job uses
GitHub's short-lived identity for keyless server signing.

A local maintainer may reproduce and inspect unsigned inputs. A community
preview or stable artifact is valid only when automation publishes its manifest
and all of these checks pass:

- the tag and embedded product version agree;
- the source commit and runtime dataset hashes agree across artifacts;
- desktop and server protocol versions agree;
- server save/replay identities match the manifest;
- the declared desktop trust model matches the artifact; and
- the Windows self-signature, publisher and absent timestamp verify; and
- installation tests exercise the produced artifact rather than rebuilding it.

MARVEL-347 owns clean-install, upgrade, interruption and downgrade verification.
MARVEL-349 owns the final two-client release-candidate journey. A failure in
either is a failed release, not permission to publish with a warning.

The desktop implementation and exact local commands are documented in
[godot-client.md](godot-client.md#desktop-artifacts). The tag-only protected
workflow is `.github/workflows/release-desktop.yml`; it requires no paid
credentials and refuses to replace an existing release.

## Required failure messages

Failure is safe only when a user or operator can distinguish what to do next.
Public errors remain bounded and never include paths, save bodies, secrets or
concealed card data.

| Condition | Required outcome |
|---|---|
| Client/server protocol mismatch | Reject as `unsupported_version`; display both product versions and protocol integers from non-secret metadata, then require matching artifacts. |
| Unknown or newer save schema | Quarantine the session as unsupported; preserve its files and direct the operator to a compatible/newer server or backup restore. |
| Replay/RNG/digest identity mismatch | Quarantine before replay; name the identity category, not hidden state. |
| Runtime dataset mismatch | Quarantine before dealing; report which dataset category differs and the expected/actual hashes in operator-only diagnostics. |
| Replay divergence | Quarantine without mutation; report the bounded divergence stage and record position. |
| Unsupported downgrade | Quarantine sessions written by the newer product; direct the operator to reinstall it or restore the matching pre-upgrade backup. |
| Signature or trust failure | Do not install or publish; identify the artifact and failed trust stage without exposing local certificate-store details. |

No compatibility failure offers “continue anyway.” A recovery action changes
the installed artifact or restores a matching backup; it never weakens parsing,
replay, authentication or signature verification.
