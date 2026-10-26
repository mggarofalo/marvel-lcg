# Doctor Strange: Iron Fist

MARVEL-621, parent MARVEL-565. This research-content packet appends actual
09014 to prerequisite revision `4131304f`; it contains no engine or public
admission change. The ability book has 679 unique rows and 34 of 40 Doctor
Strange faces. The preceding 678 raw row bodies are identical to that revision.
The remaining faces are 09015 and 09027–09031.

## Authority and interpretation

Generated `datasets/cards/cards.json` supplies Iron Fist's corrected printed
text: he enters with two mystic counters, and his optional Interrupt removes
one as its cost to stun the enemy he is attacking and deal one damage to it.
Vendored `faq:09014` changes Response to Interrupt. Current RR 1.8
`rr:consequential-damage.2.2` explicitly uses Iron Fist as its example: a lethal
interrupt aborts the basic attack when the enemy leaves play, so Iron Fist
exhausts without taking consequential damage.

The authored row uses ordinary starting counters, not Uses. Removing the last
counter leaves the ally in play. The trigger binds this attacking ally and its
exact enemy; controller authority determines the responding seat even when
another player owns the ally. Stun and damage are independent AND effects.
Tough can absorb the interrupt's one damage, stalwart blocks stun, and an enemy
already stunned still takes the damage. The interrupt applies in either identity
form. Stun replacing Iron Fist's attack and a basic thwart do not trigger it.

## Behavioral evidence

`DoctorStrangeIronFistCardTests` has twelve actual-row cases on explicitly
nonpublic boards. They cover four-resource entry and two ordinary counters;
optional acceptance/decline; zero and last counters; interruption before basic
damage and consequential damage; lethal exact-minion interruption with no
redirection to the villain; non-first-player control with matching or foreign
ownership; tough, stalwart and existing stun; and stunned-attacker/thwart
controls. The test drains the real continuation, including its single ordering
affordance, before asserting final state and empty pending work.

Existing exact inventories move to 679/34 while retaining their original row
slices and negative controls. The independent starting-counter inventory records
two mystic counters with `Uses: false`. The incomplete pack remains outside
public admission.

## Verification and limits

Evidence is outside the repository under
`C:/Users/mggar/AppData/Local/Temp/iron-fist-finish-*`.
The mutation script preserves and restores the exact original bytes. Five
lowered-row mutants failed at the intended assertions:

| Mutant | Selected cases | Failures |
|---|---:|---:|
| Response instead of Interrupt | 4 | 3 |
| Remove two counters as cost | 4 | 2 |
| Enter with one counter | 1 | 1 |
| Uses instead of ordinary counters | 4 | 1 |
| Deal the extra damage to Iron Fist | 4 | 2 |

`iron-fist-finish-preservation.log` records the raw-row comparison and candidate
ability-book SHA256. Final passing evidence is recorded in
`iron-fist-finish-final-restored-build.log`,
`iron-fist-finish-final-combined.log` and
`iron-fist-finish-cards-check.log`. The restored Content test project was rebuilt
nonincrementally with zero warnings/errors. The combined selection covers the
whole Doctor Strange classes, ability-data/lowering and rule-citation checks,
the four affected Black Widow inventory classes, and existing consequential
damage/combat-response classes. It passes 239/239 with zero skips. Offline
card regeneration matches the generated files. Normal commit hooks remain
enabled; their result is recorded in the completion handoff.

This packet establishes bounded content evidence. Full Content and broader
integration lanes, complete-pack public
setup/play/restart, native interaction and platform certification remain
integration obligations. A known Core reveal-origin harness gap belongs to the
lead's separate packet; this packet neither repairs it nor weakens its assertions.

## Independent content review

Reviewed source `deb79660` against the corrected generated 09014 text,
`faq:09014`, and current RR 1.8 consequential-damage clauses. The twelve
source cases distinguish optional counter payment, exact attack target,
controller versus owner, lethal interruption, tough, stalwart, existing stun,
counter depletion, decline, and nonattack controls. The inventory edits retain
their preceding slices and unauthored negative controls.

`DoctorStrangeIronFistReviewTests` adds two actual-book cases using a player-two
controlled, player-one owned Iron Fist. Actual Get Ready readies him twice in
one round. Both mystic counters may be spent on distinct attacks, with both
independent-effect orders exercised; the third, counterless attack still deals
basic damage and its consequential damage defeats the ally into his owner's
discard pile. Each attack drains the real continuation and leaves no pending
work. These prepared nonpublic boards do not establish public pack admission.

The review's added once-per-round-limit mutant compiles and leaves all twelve
source cases passing. Both independent cases fail at the missing second
interrupt. Exact ability-book bytes are restored, followed by a nonincremental
Content build with zero warnings/errors and 272/272 affected combined checks
passing with zero skips. Independent raw comparison
confirms all 678 preceding row bodies match `4131304f` and the candidate has
679 unique rows. Review logs and TRX results are under
`C:/Users/mggar/AppData/Local/Temp/iron-fist-review/`.

Verdict: accept the corrected 09014 row within this bounded content scope.
Complete-pack admission, full Content/integration lanes, native interaction,
and cross-platform certification remain outside this review.
