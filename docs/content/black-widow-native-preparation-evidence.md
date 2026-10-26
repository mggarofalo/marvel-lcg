# Black Widow prepared native components

MARVEL-790. Candidate starts at `3684923b` with this commit's prepared fixtures,
native scripts and manifest entries. Godot 4.7.1 .NET on Windows; native client
Debug build, managed host/tests Release, 1920×1080, one player, reduced motion.

These are explicitly prepared component scenes using the unmodified full
authored ability book. They deal the complete authored Black Widow deck through
the existing canonical scene helper, arrange actual physical cards, and expose
the resulting pending choice through the existing test-only socket factory.
They do not prove public factory admission, an opening-to-prepared-state
transcript, a complete game, another operating system or exported packaging.

`WidowNativeSceneTests` passed both managed fixture checks. Native socket
assertions independently check exact decisions and authoritative outcomes:

- Safe House #29: select, unselect and reselect one of two interchangeable
  discarded Widow's Bites. Draft changes remain at revision zero; one explicit
  commitment exhausts Safe House and moves only the selected physical card to
  hand. The other Bite and the non-Preparation resource remain discarded.
- Preparation: actual Shocker entry offers actual Widow's Bite. Its discard
  cost and two damage/stun settle before the completion window. Explicit
  Widowmaker and Synth-Suit responses deal one damage to Rhino, exhaust the
  suit and ready Black Widow, ending at revision three. No optional window is
  silently answered by the native script.

The script uses actual viewport input through the shared driver. Fixture
arrangement is separate from input; no action handlers or button signals are
invoked. The two identical discard copies are observationally interchangeable;
selection highlighting identifies the staged choice without a product need to
display internal object identifiers.

## Commands and artifacts

```powershell
dotnet build src/Marvel.Godot/Marvel.Godot.csproj
dotnet test tests/Marvel.Godot.Tests/Marvel.Godot.Tests.csproj -c Release --filter FullyQualifiedName~WidowNativeSceneTests
pwsh tools/native-cases.ps1 -GodotBin C:/Tools/Godot_v4.7.1-stable_mono_win64/Godot_v4.7.1-stable_mono_win64_console.exe -Case 'contract-widow-*' -NoBuild -ResultsDirectory <fresh-external-directory>
```

Safe House passed 100% and 150% headless and rendered profiles. Exact logs and
`results.jsonl` are outside the worktree at
`C:/Users/mggar/widow-native-final3`; earlier inspected captures are at
`C:/Users/mggar/widow-native-final1/artifacts/contract-widow-safe-house-100-rendered/`
and `contract-widow-safe-house-150-rendered/`.
Both `widow-safe-house-staged.png` and `widow-safe-house-result.png` are retained.

Preparation passed all four 100%/150% headless/rendered cases. Exact closeout
logs, `results.jsonl` and all six inspected entry/completion/result captures are
at `C:/Users/mggar/widow-native-preparation-closeout2`. Each journey made exactly
three accepted resolve requests. The prior closeout directory preserves a test
observation failure: a title-only discard lookup was ambiguous because both
interchangeable Bites were discarded. The final authoritative assertion locates
the unique initial in-play source and checks that exact card's final discard.

Earlier development runs in `widow-native-final1` and `widow-native-final2`
found automation assumptions about WindowAction versus WindowChoice and the
source action's explicit commitment. Those logs are diagnostic evidence, not
passing evidence for the final Preparation script. The final prepared scene
reuses Rhino's actual Shocker rather than allocating an extra encounter card.

## Choice explanation repair (MARVEL-792)

The Complete Choices sheet now pins the selected draft's supplied source,
costs, targets and progress above its scrolling choices, updating after draft
changes. Inspected 100% and 150% Safe House captures show exhaustion and
own-discard Preparation selection beside the staged Bite and explicit commit.
Identical discarded copies remain interchangeable; no object IDs are displayed.

Ability-window buttons expose supplied costs and effects without hover.
Widowmaker shows current damage one, the recipient's HP change and unresolved
prevention/replacement caveats. Synth-Suit shows its exhaustion cost. Its direct
ready effect has no description in this base's owning contract; that semantic
correction is a separate integration requirement. The renderer interprets no
card names, printed text or counters.

The strengthened native assertions check complete modal-summary geometry,
source/exhaustion/discard meaning and the staged Bite, plus known damage and
exhaustion on the actual response buttons. Original unstaging, revision-zero
and exact one/three explicit resolve checks remain. Captures await hover-preview
dismissal after moving the pointer through actual viewport input.

All eight focused 100%/150% headless/rendered cases passed in
`C:/Users/mggar/widow-choice-native2`. Both fresh Safe House rendered cases
passed in `C:/Users/mggar/widow-choice-capture3`; their inspected screenshots
show the complete unobscured modal at both scales. Preparation completion
captures from native2 were inspected at both scales. At 150%, the other enemy
and Synth-Suit remain below the three-choice window's initial scroll position;
after Widowmaker, Synth-Suit is directly accessible. This does not establish a
new layout design, public admission, complete game or product acceptance.

The restored focused managed run passed all 23 DecisionCopy, prepared-fixture
and actual choice-meaning checks, recorded in
`C:/Users/mggar/widow-choice-restored-focused.log`. A compiled negative control
that returned only the button label failed both the synthetic visible-meaning
check and the actual Widowmaker damage assertion
(`C:/Users/mggar/widow-choice-label-mutant.log`). Source and compiled Release
assembly were restored before the passing run.

The expanded Godot managed run encountered
`ClientCompositionTests.ExplicitSocketConfigurationDoesNotReadLocalContent`
with a host-side socket-abort IOException. Its complete failure evidence remains
in `C:/Users/mggar/widow-choice-godot-managed.log`; no baseline attribution or
fully green suite is claimed. A focused retry of that exact test passed, recorded
in `C:/Users/mggar/widow-choice-socket-retry.log`.

The initial native attempt preserves a test observation error: it required a
noninteractive Label to be an enabled input target. The final assertion checks
visible text and frame/viewport enclosure instead; diagnostic evidence remains
in `C:/Users/mggar/widow-choice-native1`.


## Independent review and compact response copy

Review identified duplicated generic and target-specific damage descriptions in
58132bcf. The final visible label retains supplied context, cost and target-specific
consequence, using generic effect text only when no specific choice description
exists. Tooltip detail remains complete. The choice-description producer admits
complete single effects, so this selection does not omit a composite effect.

Independent focused checks pass 23 cases on the compact renderer plus readiness
source 7d6ec0a7. Native Safe House review verifies unstaging clears the selected
card summary and two close/reopen cycles retain exactly one live summary, selection
and revision zero. Preparation 150% rendered checks pass with the same three
explicit commitments. The lead and reviewer inspected the final ready-response
capture: Synth-Suit, its exhaustion cost, Ready Black Widow, and explicit Pass
are visible together. Other completion choices remain accessible by scrolling;
this bounded repair makes no new whole-tableau acceptance claim.

Evidence directories: `C:/Users/mggar/widow-choice-review-lifecycle2`,
`C:/Users/mggar/widow-choice-review-compact150`, and
`C:/Users/mggar/widow-choice-review-ready150`. The native review assertions are
integrated. The separate readiness source has independent Content 30/View 4
checks and no gameplay, book or protocol changes.

Final combined branch checks: 23 focused managed checks pass after compact-copy restoration. A compiled mutant discarding the target-specific description fails both distinguishing cases (19 pass,2 fail); restored sources pass. Root integrated native Safe House100 and Preparation150 rendered both pass in C:/Users/mggar/widow-final-integrated-native. The added lifecycle script was decomposed into stage, unstage and reopen responsibilities after the complexity hook rejected its combined method; no baseline changed.
