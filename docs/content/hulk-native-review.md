# Hulk native support review

Reviewed production revision `9e49ff8943592f83595656dd52a19676ba529d86`
against `48797dd1`, with the supplied-threat View prerequisite already present.
The review adds only `SupportActionCopyReviewTests` and this record.

## Bounded verdict

The supplied support-threat display and source-named action cost distinction pass
independent review. The native source strip displays `THREAT 3` before the damage
action and `THREAT 1` after moving threat; the retained support is exhausted.
The absent initial counter is not invented. Both offered Beat Cop actions can be
distinguished by their visible supplied exhaustion/discard costs before selection.
No card-name rule calculation or hidden identity access is added to production.
Concealed source summaries return no information; the header's added field is
also guarded by `Concealed`. Existing View visibility tests remain green.

This does not establish complete Beat Cop decision comprehension. Two concrete
semantic findings remain at the reviewed revision and were reported to the
delivery owner before any production repair:

- The move action's staged explanation names Beat Cop and its exhaustion cost,
  but omits the intended movement of one threat from a scheme onto this support.
- The damage action's staged explanation says to choose an enemy to deal damage
  to, but omits the known three damage. The subsequent mandatory target prompt
  also omits that captured amount after Beat Cop has already been discarded.

These missing facts require an engine-owned semantic repair and fresh evidence.
They must not be inferred from printed text or counters in Godot. This review
does not claim whole-client acceptance or impose a routine owner approval gate.

## Independent execution

The two prepared cross-pack component scenes use actual authored 10029 through
the socket host and viewport input. They are explicitly prepared states, not
opening-to-result game transcripts. Each action has exactly two accepted
commitments: its cost and its separate target continuation. Assertions retain
the intermediate exhaustion and, for damage, source discard before targeting.
The move result changes main-scheme threat 4 to 3 and stored threat to 1;
the damage result changes Tiger Shark HP 6 to 3 while scheme threat remains 4.
Neither interaction mutates its fixture or readies Beat Cop through a helper.

Commands run independently in `C:/Users/mggar/Source/marvel-hulk-native-review`:

```powershell
dotnet test tests/Marvel.View.Tests/Marvel.View.Tests.csproj -c Release --nologo
dotnet test tests/Marvel.Godot.Tests/Marvel.Godot.Tests.csproj -c Release --nologo --filter 'FullyQualifiedName~SupportActionCopyReviewTests|FullyQualifiedName~BeatCopSourceMeaningTests|FullyQualifiedName~DecisionCopyTests|FullyQualifiedName~CardSource|FullyQualifiedName~CardMenuChoicesTests|FullyQualifiedName~RecipientCommitmentChoicesTests|FullyQualifiedName~WidowChoiceMeaningTests'
. ./tools/godot-local.ps1
$godotReview = Resolve-GodotDotNetExecutable
./tools/native-cases.ps1 -GodotBin $godotReview -Case 'contract-beat-cop-*-100-reduced' -ResultsDirectory C:/Users/mggar/Source/evidence/hulk-native-review-runs
```

Results: 430 View cases, 51 selected Godot managed cases, and both rendered native
cases pass. Native builds use current Debug Godot sources and Release test host;
profiles are 100%, 1920x1080, one player, reduced motion, Godot 4.7.1 .NET.
The manifest's rendered environment is retained. Logs are external:
`C:/Users/mggar/Source/evidence/hulk-native-review-{view,godot,run}.log`, and
`hulk-native-review-runs/results.jsonl` in that same evidence root.

The independent test additions retain supplied physical-copy names and readable
DisplayLabel overrides, distinguish null/empty/supplied costs, and preserve
explicit play and action labels and separately supplied effects. Existing
DecisionCopy, source, menu and recipient regressions remain unchanged.

## Inspected media

Reviewer-run artifact root:
`C:/Users/mggar/Source/evidence/hulk-native-review-runs/artifacts/`.
Each leaf below is within `contract-beat-cop-{move,damage}-100-reduced/` according
to its action name. SHA-256 identifies the exact PNG, independently of its path.

| PNG | SHA-256 |
|---|---|
| beat-cop-damage-entry.png | `324ea6fdf11142749e30a8cff0d0d931a08092698066c4237f0b99e1837b1e2f` |
| beat-cop-move-action-staged.png | `052dd8b796b6e86b533f251b10d7601e395709f1371326cf34b8acd82a0314cb` |
| beat-cop-move-result.png | `d3335f0b0858cef02e5d2976b8cfd318a9f69ee75622198770fb2fae99f8d230` |
| beat-cop-damage-target-choice.png | `eefa1f8d87eb6b2e3208e7eb9fc192339f866c82895235c4489584b624451a4d` |
| beat-cop-damage-result.png | `97e192613ffb3b690179de9c3aead2fa2aa783fafe17689850901a40bdce3843` |

The reviewer also inspected the author's 100% result/target captures at
`C:/Users/mggar/AppData/Local/Temp/hulk-beatcop-native-07/artifacts/` and 150%
`C:/Users/mggar/AppData/Local/Temp/hulk-beatcop-final-rendered150-beat-cop-damage/beat-cop-damage-action-staged.png`.
The 150% staged cost and commitment remain readable; its missing damage amount
is the same semantic finding. Author headless100/rendered150 run logs were read,
but their runs are author evidence, separate from the reviewer's two native runs.
Multiplayer, full-game, exported-package, and the full native matrix were not
rerun for this bounded presentation change.
