# Release installation and upgrade test matrix

This is the explicit verification matrix for the community artifacts. These
combinations are product choices; the tabletop rules do not define software
installation or compatibility. Every row uses a disposable hosted runner and
the artifact produced earlier in the same protected-tag workflow.

| Artifact | Installation identity | Trust model tested | Runtime compatibility | Persisted state | Required result |
|---|---|---|---|---|---|
| macOS desktop ZIP | SHA-256 plus ad-hoc code signature | Gatekeeper rejection is expected; the user explicitly clears quarantine | Protocol `14`, runtime dataset hashes from the release manifest | Client-local settings only | Extract, observe rejection, apply override, host and join through a real server, reconnect, remove app |
| Windows community MSIX | SHA-256 plus release-specific certificate fingerprint | Self-signed certificate is added only to `TrustedPeople`, then removed | Protocol `14`, runtime dataset hashes from the release manifest | Package-local client settings | Install, host and join through a real server, reconnect, uninstall, remove certificate and package data |
| Windows portable ZIP | SHA-256; unsigned | No certificate store change | Protocol `14`, runtime dataset hashes from the release manifest | Disposable extracted directory | Extract, host and join through a real server, reconnect, remove directory |
| Linux server image | Immutable OCI digest plus Sigstore bundle | Keyless workflow identity; no desktop trust claim | Replay `engine-replay-v2`, RNG `mt19937-iso-cxx`, digest `state-digest-v2`, protocol `14` | Empty schema `2` volume | Start healthy with separate save and diagnostics volumes |
| Linux server forward upgrade | Exact release digest over a save from lower product `0.0.0` built from compatible source | Same signed release image | Same replay, RNG, digest, protocol, and datasets; product version increases | Existing schema `2` save | Replay-verify and publish the saved session without replacing the backup |
| Linux server backup restore | Exact release digest with a stopped-volume archive restored into a fresh volume | Same signed release image | Same as forward upgrade | Restored schema `2` save | Start healthy and publish the saved session |
| Linux server interrupted candidate | Exact release digest killed after valid restore against a copied pre-upgrade volume | No weakened trust or parsing | Abrupt termination uses the last atomic generation | Interrupted volume remains recoverable; matching backup and prior image remain runnable |
| Linux server downgrade | Lower product `0.0.0` over a save last written by the release | Deliberately local test image, never published | Product version is below the save's last-writer floor | Newer schema `2` save preserved | Quarantine as `unsupported_downgrade`; direct recovery to newer image or matching backup |

Schema `1` to schema `2` migration, unknown-schema quarantine, replay/RNG/digest
refusal, dataset mismatch, atomic storage failure, and last-writer stamping are
managed integration tests because constructing those invalid states through a
public package interface would bypass the strict parser being tested. The
release workflow runs those tests before artifact installation and then runs the
artifact-level matrix above. No row claims Apple notarization, Developer ID,
CA-backed Authenticode, a trusted timestamp, or frictionless public installation.
