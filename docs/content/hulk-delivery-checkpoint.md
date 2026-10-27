# Hulk delivery checkpoint

Canonical parent: MARVEL-566. Complete scope is the verified
[33-face handoff](research/remaining/hulk-implementation-handoff.md).
Current local assembly: **709 authored rows, 24/33 Hulk faces**. No public Hulk
admission or full-pack acceptance. Ordinary content acceptance remains agent-owned.

The branch starts from frozen Doctor Strange candidate `553869be`; PR454's final
CI remains pending. Local nonpublic integration proceeds during that run. Public
Hulk admission and merge preparation follow complete pack coverage and Strange
acceptance. This explicitly supersedes the earlier plan to wait before assembling
any Hulk prerequisites.

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
This latest 709-row assembly has not yet received full-pack certification.

## Remaining work

- MARVEL-846: all eight signature faces are reviewed and locally integrated.
- MARVEL-847: eight starter faces are reviewed and locally integrated. Toe to Toe10015
  remains absent. Engine source `b2f45d9` passed full Content7606, but independent
  review found its retained attack loses a lethal defeat receipt, suppressing the
  existing Chase Them Down response. A damage-only control passes. The engine pair
  must repair and independently close that concrete finding before actual-row authoring.
- MARVEL-849: actual obligation/nemesis10025–10028 (four faces), including full
  Clash reveal/cleanup using the reviewed ranked-attack primitive.
- MARVEL-850: customization10029–10032 (four faces), now in bounded authoring from the reviewed 709-row book.
- MARVEL-851: verified public setup, characteristic actions, complete games,
  restart/replay and final combined regression/platform evidence after all33
  faces are ready.

The six card packets cover3+8+9+5+4+4=33 faces exactly once. The final complete
book is expected to contain685 predecessor faces plus33 Hulk faces:718 total.
No marketing packaging reconciliation or blanket human content gate is added.
A genuinely new UI needs inspected media and a focused Plane review item.
