# Black Widow replay v27 integration review

Reviewed commit: `3c5bd7058568fdd95a67e558442ab395287f06b7`.
The isolated review branch is `review/bw-replay-v27`.

This review covers replay identity admission and publication, release identity
consistency, named migration guards, and preservation of historical fixtures.
It does not establish native UI acceptance, packaged upgrades, cross-platform
certification, or complete played-campaign compatibility.

## Findings

No production defect was found in the reviewed boundary. Runtime identity,
CI image checks, release provenance, server upgrade checks and campaign script
defaults consistently name replay v27, protocol 41 and session schema 7.
The built server's `--version` output independently confirms:

```text
v0.1.0-dev.0 · engine engine-replay-v27 · protocol 41 · save 7
```

The linked save-upgrade document described v26 as the current delivery. The
review corrects its current identity and source-admission statement while
retaining the historical v26 behavior and destination-bound route description.

The v19/v20/v21 to v27 adapters call ordinary complete strict replay before
replacing compatibility metadata. Retained inactive history and the active
pending prompt remain covered by `SessionReplay.Verify`. Campaign upgrades
likewise verify the aggregate before replacing metadata in its retained proofs
and attempts. No v26 to v27 source admission is present.

## Independent evidence

`LiveEffectsReplayRouteTests` adds eleven focused cases:

- Each named v19/v20/v21 to v27 adapter rejects a v26 destination before opening
  gameplay and leaves its input unchanged.
- Each current admitted source rejects an altered original RNG count without
  replacing that count or publishing upgraded metadata.
- V22 through v26 source identities have no current route with either the
  frozen predecessor dataset triple or the current dataset triple.

The actual archived v19 session supplies replay facts. V20 and v21 inputs are
explicitly verified intermediate derivations that preserve every v19 fact;
they are not represented as historical v20/v21 recordings. The unsupported
source probes are synthetic compatibility records, not gameplay recordings.

The existing replay-contract, engagement-route and engine-identity tests are
included in the focused run. The community release-policy tests independently
check source, workflow and release-contract agreement.

`git diff --exit-code main 3c5bd705 -- 'tests/**/Fixtures/**'` succeeds with no
differences. Thus the entire committed fixture tree, including historical
archives, remains byte-identical to main. The v27 commit also changes no dataset
or fixture path. No archived expected gameplay fact is regenerated.

The focused Windows server run passed 45/45 tests, including all eleven new
cases. Community release-policy tests passed 8/8. These runs built the affected
projects in Release configuration. External logs and TRX files are under
`C:/Users/mggar/AppData/Local/Temp/marvel-bw-v27-review/`.
