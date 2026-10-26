# Actual Lockjaw face

05018 adds the ordinary own-discard permission, bringing the authored book to
575 faces and 33 of 34 Ms. Marvel faces. Printed authority is the joined card
text: play from your discard pile during your turn, paying the resource cost.
The reviewed prerequisite preserves ordinary cost, discounts, uniqueness,
ownership, ally limit and entry timing; the discarded ally is not a resource.

DiscardPlayDecisionTests now uses the full ability book for the printed
permission. Both second-seat payment variants and four published-offer rejection
controls run against the actual row; the synthetic entry-response variants stay
isolated because Lockjaw has no printed entry response. Existing prerequisite
reviews independently prove live payment purity and single commitment, including
actual Core Nick Fury entry after the ally-limit choice.

The actual-row/permission/card-data filter passes 192 cases. Removing only
Lockjaw's authored permission produces six expected failures in the actual
second-seat and rejection cases; all four synthetic entry controls still pass.
Original dataset bytes are restored and all 192 cases pass again. Logs are
`C:/Users/mggar/Source/lockjaw-actual-{mutant,final}.log`.

Independent review of 05018 at 8d235b01 agrees with the joined printed text,
the exact Constant/this permission row and the full-book decision tests. The
vendored post-RRG-1.5 ruling 154 distinguishes discard play from treating a card
as in hand (Team-Building Exercise cannot fetch discarded Lockjaw). No actual-row
correctness finding remains. Public Ms. Marvel admission is closed.

The characteristic native proof is registered as
`contract-lockjaw-discard-100-reduced` in representative and exhaustive profiles.
Its explicitly prepared cross-pack scene uses actual 05018 and the full authored
book with Spider-Man; it does not claim legal public Ms. Marvel setup. Real
viewport clicks choose Play Lockjaw through All actions, stage Energy and Genius,
and explicitly Pay and play Lockjaw. Native and host assertions establish zero
revision changes while staging, exactly four resources, one resolve, the same
physical card moving from discard through revealing into allies, payment cards
discarded and the next PlayerAction prompt. Lockjaw is never a resource.

Windows evidence uses Godot 4.7.1 .NET, Debug client and Release managed helper,
1920x1080, 100%, reduced motion. The manifest run is
`C:/Users/mggar/Source/lockjaw-native-final-headless/results.jsonl`. The direct
minimized rendered run uses `MARVEL_NATIVE_RENDERED=true`,
`MARVEL_NATIVE_CONTRACT=lockjaw-discard`, the installed `GODOT_BIN` and
`MARVEL_UI_EVIDENCE_DIRECTORY=C:/Users/mggar/Source/lockjaw-native-final-rendered`,
with `dotnet test tests/Marvel.Godot.Tests/Marvel.Godot.Tests.csproj -c Release
--no-build --filter FullyQualifiedName~NativeContractJourneyTests`.
The payment/result PNGs were inspected: answering Spider-Man, Play Lockjaw,
Pay 4, both selected resource cards, four-resource progress, explicit commitment
and the resulting 4/4 ally are readable. Linux, packages and owner product
acceptance are outside this local evidence.

Separate presentation finding: All actions currently groups discarded Lockjaw
under Hand because CompleteChoiceGroups assigns ordinary Play to that heading.
The action and payment work, but this heading misstates the source zone. This
native proof does not certify the heading as correct or waive its correction.

## Integrated source-group correction

The complete-choice catalogue now groups ordinary discard play under **Discard
pile** using the authorized source zone. Hand plays remain under Hand; an
unknown source is not labelled as a hand. The two regression rows fail against
the old categorizer and all eleven organization checks pass with the fix.
The registered native case additionally requires the Discard pile heading before
selecting Lockjaw. Headless and minimized rendered runs pass at 1920x1080,
100 percent scale, reduced motion; the lead inspected the actual source-choice
capture, which visibly separates hand plays from Lockjaw's discard play.
Evidence: `C:/Users/mggar/Source/lockjaw-source-group-headless/results.jsonl`
and `C:/Users/mggar/Source/lockjaw-source-group-rendered-100/`. An initial direct
rendered invocation omitted the manifest scale/viewport and failed at setup under
an inherited 80 percent profile; the explicitly configured run above is the
pixel evidence. No gameplay or public admission changes are part of this fix.
