# Independent review of Doctor Strange setup

Reviewed production revision: `a8e71f5b`. Verdict: rejected pending the narrow
identity-detection repair below. This review changes tests and this evidence
record only, on `review/strange-setup` in the isolated
`C:/Users/mggar/Source/marvel-strange-setup-review` worktree.

`InvocationSetup.ValidateSeat` recognizes Strange only when an identity spec
contains `09001a`. A direct blueprint carrying only `09001b` with the entire
Invocation block absent therefore returns a world. The insert requires Strange
to begin each game with the separate five-card deck, and this setup boundary
claims to reject malformed identity/composition before allocation. Recognizing
either Strange face must enter the existing exact-pair and exact-five checks.
`PartialIdentityCannotHideMissingInvocationDeck` reproduces the bypass and is
intentionally failing on the reviewed production revision. The valid dealer
path still requires both identity forms; the finding concerns hostile direct
blueprints, not public admission.

The independent reviewer inspected the mc08 rulesheet transcription, its pinned
provenance and the original local divider image. Its physical quantities match
the test-local 15 + 17 + 8 ordinary cards and five separate single Invocations.
Source inspection confirms validation precedes world allocation, binding uses
final blueprint indices, enum values append, and shuffles follow the ordinary
player/encounter shuffles before scenario abilities. Existing accepted-game
fingerprint/RNG controls pass. Public admission and save/replay remain deferred.

Six new independent cases cover caller storage mutation and reverse authored
order, first/middle Strange seats followed by another player's allocation block,
whole-deck owners -1 and 2 in a two-seat game, and the partial-identity bypass.
Five pass and the bypass fails. The complete Doctor Strange setup classes plus
Content citation tests total 30 cases: 29 pass, one fails solely at the new
expected exception. Whole Rules setup lifecycle, Invocation deck/boundary,
Doctor Strange authority and citation classes pass all 28 cases.

The compiled snapshot mutant changes `Cards = cards.ToImmutableArray()` to
`Cards = cards`. The new immutability case fails with expected `09036` versus
caller-mutated `01001a`. A `finally` restores the original source, a Release
`--no-incremental` build passes, and that same case passes. Logs live outside
Git at `C:/Users/mggar/Source/strange-setup-review-*.log`: `focused`, `content`,
`rules`, `mutant-snapshot`, `restored-build` and `restored-snapshot`.
Architecture and normal commit-hook output use companion `architecture` and
`commit` logs. Closure requires the narrow production fix, the failing case
becoming green, and final affected regression evidence.

The original prerequisite record contains a pre-existing invalid UTF-8 byte at
offset 605. This independent evidence record leaves its source bytes intact;
the integration owner has been notified to repair that encoding separately.

## Lead repair and integrated check

The integrated correction recognizes either Doctor Strange identity face before
requiring the exact pair and five-card recipe. All 30 setup/review/citation cases
now pass, including the previously failing alter-ego-only/no-deck case. Evidence:
`Source/evidence/strange-setup-integrated-closure.log`. Independent closure
review remains pending; public admission remains closed.

The Godot encounter-replacement fixture now supplies the exact dealt card and
queue origin already represented in its physical setup. Its three existing
meaning/privacy assertions pass unchanged. The initial build required adding
the Timing namespace; the restored run passes. Evidence:
`Source/evidence/strange-godot-reveal-origin-fixture-restored.log`.
