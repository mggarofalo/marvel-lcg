# Black Widow final response rows

Packet base: `29aa293f`; branch `feat/widow-final-two-faces`. This packet authors
08030 Counterattack and 08031 Rapid Response only. It contains 634 ability rows
and 23/34 Black Widow faces. The independently reviewed seven-face and four-face
packets are not part of this branch; their later integration completes the face
inventory. Public admission, setup, engine mechanisms and version identities are
unchanged. Whole-pack acceptance remains separate.

The complete prior 632-row text prefix is byte-identical: 634,313 bytes,
SHA-256 `ee2f7b03a292168bb0d3079ff10d0f66d378108b61ebcbd765bcf27d36dfe2bd`.
The only JSON difference is the two appended rows. Neither prints starting
counters. The explicit ability-data face inventory and historical batch-position
assertions include these additions without shifting the prior batches.

## Printed authority and current adjudication

Generated `datasets/cards/cards.json` is the printed authority. Counterattack:
"Max 1 per player. Hero Response (attack): After you take damage from an enemy
attack, discard Counterattack → deal an equal amount of damage to that enemy."
Rapid Response: "Max 1 per player. Hero Response: After an ally you control is
defeated, discard Rapid Response → put that ally into play from your discard pile
and deal 1 damage to it." The printed maximum is enforced by ordinary upgrade
admission; the rows express their complete triggered abilities.

Current RR 1.8 `attack-enemy-activation.step.6.a` and `.6.b` finish the attack
before forced damage responses/retaliate and then optional Counterattack. Older
rulings `47681f33e542f477` (pre-1.5) and `7ce7004ea74ed42a` (1.6) describe a
superseded order. The RR 1.8 procedure governs, as reviewed in
[counterattack-final-review.md](counterattack-final-review.md). The damage amount
is the attack's captured actual placement on this identity, not damage dealt or
boost-ability damage. Same-title villain succession retains the character under
`villain-defeat.3.1`; a departed/reentered minion is another copy.

Rapid Response uses the defeated ally's captured controller, exact physical card
and incarnation, with current membership in that player's own discard. The
pre-1.5 Red Dagger ruling `c511a191fe8cc74d` confirms a card currently in hand is
unavailable. Current `leaves-play.1` governs reentry; an out-of-play roundtrip
alone does not create a different incarnation. Ordinary ally entry and its
responses finish before the one-damage suffix. No MovementVersion is used.
The vendored MarvelCDB FAQ has no entries for either face. Prior mechanism and
ruling review is recorded in
[black-widow-response-composition.md](black-widow-response-composition.md).

## Actual-row verification

The existing Counterattack and Rapid Response composition/receipt/entry fixtures
now load the canonical rows without test-local replacements. They distinguish
partial/full prevention, Tough, other recipients, boost damage, forced timing,
same/different-title promotion, minion reentry, decline, invalid answers,
ownership versus captured controller, discard roundtrips, entry suspension,
one-HP re-defeat, and Preparation completion versus Stun cancellation.
Preparation/Bite/Synth-Suit continuation suites are included in the focused run.
The new final-row tests add real and invalid extraneous resource selections on
Rapid Response, checking digest, pending work, occurrence identity, RNG state and
consumption, events and retry; another control reenters the paid source while a
nested ally-entry choice is suspended and verifies the original completed
Preparation source incarnation.

Both existing durable review factories now execute canonical 08030/08031 rows.
Counterattack retains a synthetic attack Action and simplified Synth-Suit row;
Rapid Response retains a synthetic defeat Action and ally-entry choice. Their
strict JSON/fresh-host tests remain nonpublic mechanism journeys, not public
Black Widow setup evidence.

On this packet, 231 focused Content tests pass in Release. Command filter:
`Counterattack|RapidResponse|BlackWidow|AbilityData|Preparation|SafeHouse`.
Log: `C:/Users/mggar/Source/widow-final-two-content.log`. An initial run passed
226 and failed the explicit permitted-face inventory; that list was updated.
A subsequent compile caught a fixture zone spelling and array analyzer finding;
both were corrected before the passing run. No full managed, native UI or
cross-platform acceptance claim is made.


All six canonical-row durable restart cases pass in Release (four Counterattack,
two Rapid Response): `C:/Users/mggar/Source/widow-final-two-server.log`.
A compiled/loaded Counterattack row mutant substitutes attack damage dealt for
actual received damage. Its unchanged partial-prevention case fails with
expected 1 / actual 2; the other two cases pass. The build succeeds with zero
warnings/errors. Logs: `widow-final-two-mutant-build.log` and
`widow-final-two-mutant.log` under `C:/Users/mggar/Source/`.
Restored sources are forced through a non-incremental Release build before
repeating the 231-test focused run; the final result is recorded in
`widow-final-two-restored-build.log` and `widow-final-two-restored.log` there.

The restored non-incremental build passes with zero warnings/errors; all 231
focused checks pass again (21 seconds). Normal pre-commit hooks remain enabled.
