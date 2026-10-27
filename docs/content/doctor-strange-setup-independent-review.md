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

## Independent closure review

Reviewed integrated production revision: `b0847e10cb2cf82ccd7e128ba60838ddef7e9263`
from `review/strange-setup-closure` in the isolated
`C:/Users/mggar/Source/marvel-strange-setup-closure` worktree. Verdict: accepted
for the typed setup prerequisite. The sole production repair relative to the
reviewed `a8e71f5b` Invocation validator recognizes either exact Strange face;
the exact paired identity, exact five single Invocations, selected owner and
separate-slot checks remain intact. Validation still precedes `new World`,
card/area allocation and setup events. No public admission, canonical setup,
ability book, engine source or existing expected result changes were made by
this closure reviewer.

The original single-face/no-deck regression is retained within
`PartialIdentityCannotBypassInvocationComposition`, now parameterized over both
`09001a` and `09001b`, each with and without the five-card block. All four
malformed identities reject with no emitted setup events. The complete Doctor
Strange setup, independent review, compatibility and Content rule-citation filter
passes 33/33 cases on the integrated sources plus these reviewer controls. The
prior immutable-input, physical binding, exact composition and accepted-product
fingerprint/RNG assertions remain unchanged. Existing compiled-mutant evidence
above remains applicable; this closure introduces no production decision.

The encounter-replacement fixture correction matches the production
`RevealNextEncounterCard` operation: the origin names the same physical card as
`Subject`, records its actual dealt-encounter queue, and marks that queued card
as dealt. This supplies the required causal fact rather than bypassing the
engine qualifier. All three existing meaning/privacy cases pass with their
assertions unchanged. This is managed fixture verification, not native or
whole-product acceptance.

Closure commands (Release) and external evidence:

- `dotnet test tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj --filter
  'FullyQualifiedName~DoctorStrangeSetup|FullyQualifiedName~RuleCitationTests'`:
  33 passed; `C:/Users/mggar/Source/strange-setup-closure-content.log`.
- `dotnet test tests/Marvel.Godot.Tests/Marvel.Godot.Tests.csproj --filter
  'FullyQualifiedName~EncounterReplacementMeaningTests'`: 3 passed;
  `C:/Users/mggar/Source/strange-setup-closure-godot-meaning.log`.

The final reviewer test rename was rebuilt and all 9 independent review cases
pass (`C:/Users/mggar/Source/strange-setup-closure-controls.log`). Normal commit
hooks are captured in `C:/Users/mggar/Source/strange-setup-closure-commit.log`.

The prerequisite evidence document now decodes with a strict UTF-8 decoder;
its multiplication signs, en/em dashes and curly punctuation are intact, with
no replacement characters or common mojibake sequences. The prior encoding
finding is closed. Public pack admission, complete gameplay, save/replay and
native acceptance remain outside this prerequisite verdict.