# Hulk delivery checkpoint

Canonical parent: MARVEL-566. Complete scope is the verified
[33-face handoff](research/remaining/hulk-implementation-handoff.md).
Current local assembly: **718 authored rows, 33/33 Hulk faces**. No public Hulk
admission or full-pack acceptance. Ordinary content acceptance remains agent-owned.

The branch starts from frozen Doctor Strange candidate `553869be`. Its later
runtime at `a1870412` passed all eleven CI jobs in run40451138391. Strict
up-to-date protection requires a Markdown-only synchronization and another CI
run before Strange merges; a Git path audit proves runtime files unchanged.
Local Hulk public setup/admission verification now proceeds after all33 faces
are independently reviewed. Hulk merge still follows Strange merge. This
supersedes the earlier requirement to wait for Strange merge before any local
public-path work; no partial pack or new public bypass is introduced.

## Reviewed and integrated packets

| Packet | Source | Independent review | Scope |
|---|---|---|---|
| H1 / 843 | `9a143090` | `43a2e812` | Actual Hulk/Banner and Leap, three faces |
| P1/P3 / 844 | `a88a0935` | `e59755cf` | Physical adjusted printed price; Attack-trait event resources |
| P2 / 845 | `08619549` | `0919e81d` | Overkill belongs to one basic attack occurrence |
| Basic / 848 | `fc9a01b6` | `52ec4dd2` | Actual 10020–10024, five faces |
| Q3 / 853 | `f45a6d39`, repair `697de00b` | finding `acb92681`, closure `e2a5d352` | Explicit live hand-resource eligibility before and during payment |
| Selected target / 852 | `fd23bf88`, repair `b001e377` | finding `1c6f36fa`, closure `2bccd3fb` | Current controller owns chosen target's attack; lost explicit target never substitutes resolver |
| Signature subset / 846 | `86412c83` | `10336f30` | Actual 10002,10003,10006–10010, seven faces |
| Ranked attack / 852 | `c6621429` | `b68c7878` | First player selects one ranked enemy, retaining the selected character |
| Bounded ordinary damage / 846 | `7a98feea`, repair `65190efe` | finding `5fea29de`, closure `b3c9d8c` | One bounded recipient group; accurate later-choice descriptions |
| Pricing-family layout | `7aa42043` (isolated remedy) | `38b271f2` | Six pricing variants extracted; no operation or JSON change |
| Starter subset / 847 | `6e758f7d` | `3588d6ac` | Actual 10011–10014 and 10016–10019, eight faces |
| Labelled activation / 847 | `b2f45d97`, repair `3a016518` | finding `633ce5fc`, closure `ca700082` | Original attack damage/defeat receipts across chosen enemy activation |
| Support threat movement / 850 | `61d0973e` | `e1ae44f0` | Exact source-support threat storage and removal continuation |
| Customization subset / 850 | `3c6bb5bc` | `546f42b8`, evidence closure `3b27745f` | Actual 10030–10032, three faces |
| Obligation/nemesis / 849 | `a07d4305`, root Clash query correction | `022c8254` | Actual 10025–10028, four faces |
| Final two / 847,850 | `163b1f66` | `d06a1c6f` | Actual Toe to Toe10015 and Beat Cop10029 |
| Thunderclap / 846 | `4dcd8afb` | `42111a07` | Actual 10005 bounded ordinary damage, one face |

Packet documents preserve the authority, source/review evidence and limitations.
The independently accepted signature subset preserves all 693 predecessor objects
byte-identically. The two concurrent additions to `AbilityEffect.cs` were reviewed
at integration: only `Damage.Maximum` and `ActivateEnemies.ChooseOneEnemy` change
their separate record blocks. No broad record replacement occurred.

## Combined local evidence

All logs below are under `C:/Users/mggar/Source/evidence/`:

- H1/payment/basic-use assembly: 153 Content tests;
  `hulk-integrated-prerequisites.log`.
- Basic/Q3 combined assembly: 212 Content tests;
  `hulk-integrated-basic-hand-resource.log`.
- Selected-target repair: 236 activation/attack-chain cases;
  `hulk-integrated-selected-owner.log`.
- Actual signature integration: 465 focused cases;
  `hulk-integrated-signatures.log`.
- Ranked choice integration: 84 cases; `hulk-integrated-ranked-attack.log`.
- Bounded ordinary damage plus ranked/status/description regression: 225 cases;
  `hulk-integrated-bounded-damage.log`.
- Complete 18 inventory-owning classes plus AbilityData/citations: 156 cases;
  `hulk-global-inventory-closure.log`.

Integration inspection caught sixteen inherited whole-book assertions still at
693 and negative controls naming now-authored 10002. Those counts now assert700;
unknown controls name existing catalog face90001, which remains unauthored.
Original pack positions, exact face sets and counter assertions remain intact.
The first closure run found two additional array-form negative controls (154
passed, two failed); both are repaired in the final156-pass run. The global exact
authored inventory remains enforced. These are fixture maintenance corrections,
not relaxed content coverage or gameplay changes. Future authors must search
whole-book counts and newly authored negative IDs and run every affected inventory
class, including initially unchanged classes.

The reviewed pricing remedy was applied to exact staged tree `eb14ac4d`, producing
source tree `9fce6b9b` byte-for-byte. That 700-row candidate passed all 7,670 Content
checks, full architecture 129 and a warning-free Release solution build. Independent
layout review passed architecture 129 and 193 focused Content cases. Root integration
commit is `88b6fa87`; the review document follows in its own commit.

The 709-row combined book is byte-identical to the independently reviewed Thunderclap
source. Sixteen starter integration conflicts were proved to differ only in whole-book
700→708 counts (plus two terminal blank lines in one file), then advanced to709 with
the reviewed Thunderclap row. Pack slices and 90001 unknown controls remain intact.
The combined inventory/Hulk/bounded-damage/ranked-choice run passed 314 cases;
`hulk-24face-integration.log`. Source/review packet runs remain linked in their documents.
The later 712-row combined book is also byte-identical to its reviewed source.
Combined inventory/Hulk/activation/threat movement/bounded damage/ranked-choice
checks pass411 (`hulk-27face-integration.log`). Before those card additions, the
support prerequisite passed89 integrated cases plus6 independent review cases
(`hulk-integrated-support-threat.log` and `hulk-integrated-support-review.log`).
The customization THEN-mutation question is closed: an isolated serial run kills
that mutation on the undamaged/exhausted ally while both damaged controls pass.
The earlier survivor was contaminated by competing-process apphost locks and is
excluded, not classified equivalent. Restored nonincremental build and all211
source/review affected cases passed again; `3b27745f` records that correction.
This latest assembly has not yet received full-pack certification.

## Complete authored candidate and remaining product proof

All six card packets are independently reviewed and locally integrated:
3+8+9+5+4+4=33 faces, with685 predecessor faces plus33 Hulk faces =718 rows.
The final book is byte-identical to the reviewed two-card source, SHA-256
`8a8935c1e8256354100ea185834a64ba7768cf77470012f3d673955e473ec189`.
All716 predecessor objects were independently compared byte-for-byte; both
Beat Cop injection fixtures now require the actual authored book.

Final-two source verification passed1064 affected cases and20 restored actual/
citation checks; independent review passed969 affected cases and23 restored
focused checks. Valid actual damage and counter-kind mutants were killed.
A canceled broad run and a THEN-shaped admission rejection are explicitly
excluded as behavioral mutation evidence. Root's final combined Hulk/activation/
threat/ranked-choice/ability-data run passed263 cases
(`hulk-33face-integration.log`). Normal hooks preserve the existing gates.

Public admission is integrated as `48797dd1`, independently reviewed as
`766c6cf0`: exact40 starter, all33 faces, nemesis quantities, legal extras and
owner/copy limits, both public transports, and predecessor preservation.
Source setup1429, Server69, restored Hulk110 and architecture129 passed.
Independent admission111 and View29 passed, including actual support threat
projection and hidden-card controls. See [admission](hulk-public-admission.md)
and [independent review](hulk-admission-independent-review.md).

The support threat display repair `4d535790` passed all430 View tests and a
compiled original-formatter mutant killed all four distinguishing cases.
Its [rendered interaction proof](hulk-support-threat-presentation.md) remains
separate. Public games `314510bf` contain eight fixed complete games with exact
independent-host accepted decisions and strict terminal replay. A test-policy
control was corrected and separately passed after a compiled mutant was killed;
the earlier TRX's control failure is not represented as a fully green run.
See [game evidence](hulk-public-games.md).

Broad unit verification at `48797dd1` passed7936 Content,430 View,84 Session,
51 Core and50 Decisions tests. Rules reported18 failures from the omitted Hulk
setup authority. The correction `5b637305` adds its reviewed outside-Core
adjudication, regenerates the catalog and preserves exact source/obligation
inventories; all1187 Rules tests and the offline catalog check then passed.
Logs: `C:/Users/mggar/Source/evidence/hulk-public-integrated-unit.log` and
`hulk-authority-{check,rules-restored}.log`. This is explicitly assembled
evidence across the named revisions, not final same-head certification.

MARVEL-851 now has independent complete-game/fresh-host continuation proof and
rendered native interaction proof. Final combined managed/platform CI and merge remain.
The current public candidate is not yet a merged accepted pack. No marketing
packaging reconciliation or blanket human content gate is added. A genuinely
new UI requires inspected media and a focused Plane item.

## Integrated public and native evidence

Fresh-host review `b161c2fa` independently verifies Unstoppable Force before and
after payment/action and Thunderclap before payment and recipient choice. Stored
strict JSON loads into a fresh factory/host; invalid or stale input is atomic,
subsequent decisions match exactly and costs are not charged twice.

Acceptance at `5e28dcab` passed 189/189. The earlier broad integration run passed
36 Release, 129 Architecture and 981 Godot tests (seven opt-in native entry cases
skipped). It reported five Server failures: four stale admission expectations
were corrected in `1f977d48`; an unchanged variable-payment socket test passed
focused after cancellation under broad load. Focused seven Server and four
Doctor Strange negative-control cases pass. That original broad run is not
claimed green; final CI must certify the integrated head.

Native source `9e49ff89` and independent review `e612e273` verify visible support
threat, named exhaust/discard costs, move results and damage results. Semantic
source `e50a87fe` supplies engine-owned known effect descriptions. Independent
review `d076836c` adds native meaning assertions and four rendered journeys at
100%/150%, but rejects loss of known damage on legal empty-deck recycling.
Repair `49ca46db` captures public source provenance across costs and continuation;
its compiled mutation fails four distinguishing cases, with exact restoration,
21 Godot, 13 Content and 84 Session cases passing. The review closure separately
verifies both visibility policies and actual completed three-damage resolution.
See [repair evidence](hulk-public-provenance-repair.md) and
[independent semantic review](hulk-damage-meaning-review.md).

Main synchronization `a1369d37` includes every upstream change since the shared
Strange ancestor: platform timeout budget and three documentation files. The
four-path audit is in `C:/Users/mggar/Source/evidence/hulk-main-sync-paths.txt`.
The complete Hulk candidate still requires final CI and merge; authored row count
and exact book hash above remain unchanged.

Final semantic closure `56e9684d` is accepted and integrated as `bcffcaac`.
On that integrated runtime, 21 focused Godot semantic cases and 13 Content cases
pass (`hulk-final-semantic-{godot,content}.log`). Final certification is delegated
to the existing required CI lanes rather than repeating earlier broad local runs.
