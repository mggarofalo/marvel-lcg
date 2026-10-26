# Green Goblin integration proof review

Reviewed 2026-10-26 at `19763377` plus native Bruno proof `0c2be02a` (local
cherry-pick `c3ecb8b3`), with the integration candidate's abstract
`IWindowAbilities.PrepareWindow`, explicit virtual `NoCardAbilities` forwarder,
and actual-full-book conditional-status test fixture correction copied exactly.
The review changes only strengthen restart evidence; no product interaction changes.

## Focused managed evidence

Release commands used `--nologo -m:1 -nr:false` in an isolated worktree.
Logs are in `C:/Users/mggar/AppData/Local/Temp/`.

| Project and `FullyQualifiedName` filter | Result | Log |
|---|---|---|
| Architecture: `CardAbilityBoundaryTests` | 3 passed | `integration-review-boundary.log` |
| Rules: `Window` or `NativeRetaliation` or `NativeIndirectRetaliation` | 69 passed | `integration-review-rules.log` |
| Rules: `OfferingTests` or `NativeAbilityIdentityTests` | 13 passed | `integration-review-offering.log` |
| Content: `Payment` or `BoundedCardRetrievalTests` or `NoCardAbilitiesCompletionTests` | 292 passed | `integration-review-content-corrected.log` |
| Acceptance: `GreenGoblinRestartTests` | 4 passed | `integration-review-restart-exact.log` |

The first Content run found nine duplicate-authored-card fixture failures for
05015/05031. Replacing the equivalent test-local rows with `AuthoredCards.Runner()`
retains the behavioral assertions and exercises the actual full book; the corrected
292-test run is green.

## Exact scope verdicts

1. **Window preparation: accepted for the reviewed boundary.** The interface has
   no default body; the no-card resolver explicitly forwards through virtual
   `Resolve`. Authored decorators forward preparation, while native windows retain
   their own resolver. Focused boundary, ordinary-window, native retaliation and
   payment/retry controls pass.
2. **Restart: accepted for all four public modes and recorded seeds.** Risky
   Business standard/expert and Mutagen Formula standard/expert compare each
   persisted prompt, durable answer, event sequence, fingerprint and RNG word count
   to an uninterrupted control. Restarts during dependent choices preserve exact
   sync and saved authority. Invalid input preserves both, and the loop continues
   with the matching legal answer. This is a bounded 100-decision journey per mode,
   not complete-game or exhaustive-seed coverage.
3. **Bruno native retrieval: accepted as a synthetic cross-pack component proof.**
   The real full ability book and actual Bruno support operate on four physically
   stored cards. Native controls stage, unstage and explicitly commit zero, one
   and three targets. Server witnesses check two commits, exact selected identities,
   hand growth, exhausted Bruno, concealed remaining stored cards and concealed
   deck faces. The fixture does not establish Ms. Marvel product admission.

## Native artifacts and visual inspection

The review independently rebuilt the Debug Godot client and Release test assembly,
imported the fresh worktree, and ran `NativeContractJourneyTests` with
`MARVEL_NATIVE_CONTRACT=bruno-0`, `bruno-1`, and `bruno-3`. All three passed using
Godot 4.7.1 .NET, headless, 1920x1080, scale 100%, motion disabled, one player.
Logs: `integration-review-native-{0,1,3}.log`; build logs:
`integration-review-native-build.log`, `integration-review-native-testbuild.log`.

The author logs `bruno-split-headless-{0,1,3}.log` also pass. The reviewer opened
`bruno-native-rendered-3/bruno-3-selection.png` and inspected the image: Bruno's
source, answering Spider-Man seat, up-to-three/none instruction, three checked
physical cards, their selected names and the named add-to-hand commitment are
readable without clipping at the recorded 1920x1080/100% profile.

No remaining correctness finding in these three proof scopes. Native case manifest
membership still requires integration. Other scales, motion profiles, multiplayer,
exported packages and owner product acceptance were not reviewed here.
