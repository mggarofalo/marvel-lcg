# Independent review of the final response rows

Reviewed source: `e58d0f003465afe57553e7737ba135ef74fbdadd`, against
`29aa293f`, in an isolated worktree. Verdict: accept the two actual rows,
08030 Counterattack and 08031 Rapid Response. No blocking production finding.
This review adds tests and this evidence record only.

Printed authority was read from `datasets/cards/cards.json`. Both rows preserve
their Hero Response form restriction and discard-source cost. Counterattack
retains its attack label, original enemy binding and actual received attack
damage. Rapid Response requires the captured ally controller, exact defeated
physical card/incarnation and current own-discard membership, then ordinary
entry before its one-damage suffix. The independent mechanism reviews remain
the authority for the prerequisite implementation; this packet checks their
composition with the canonical rows rather than reopening that entire history.

Current RR 1.8 `attack-enemy-activation.step.6.a`/`.6.b` places forced
damage-taking responses and retaliate before optional Counterattack. Vendored
pre-1.5 ruling `47681f33e542f477` and 1.6 ruling `7ce7004ea74ed42a` do not
override that current explicit procedure. Same-title villain succession is the
same character under `villain-defeat.3.1`; different-title succession is not.
Rapid Response's Red Dagger ruling `c511a191fe8cc74d` excludes the card while
currently in hand. Current `leaves-play.1` governs new copies; an entirely
out-of-play discard roundtrip does not create one. The FAQ snapshot has no
entries for either face.

The original 632 rows are semantically unchanged. Independently compared the
raw 634,313-byte prefix with the base: byte-identical, SHA-256
`ee2f7b03a292168bb0d3079ff10d0f66d378108b61ebcbd765bcf27d36dfe2bd`.
The candidate has 634 unique rows and 23 Black Widow faces. Both new rows lack
starting counters. Existing inventory changes retain historical batch positions.

The timing marker now uses 09020, printed Unflappable, an otherwise unauthored
Upgrade. Its forced Tough ability is explicitly synthetic; it neither replaces
08030/08031 nor claims to implement Unflappable. The original final observations
could also pass with the marker resolving before attack completion. Review
strengthens that test to observe the actual Tough attachment after the
`AttackCompleted` event. That stronger ordering assertion passes.

Four new canonical-row controls distinguish hero and alter-ego eligibility for
each face under `rr:ability.13`, with otherwise identical positive controls.
Counterattack's fixture explicitly schedules an attack independently of normal
activation selection. Both forms take the same attack damage, but only hero
form receives the optional offer. Rapid Response likewise receives an offer
only in hero form after the same ally defeat. Declines leave sources unpaid.

Independent Release verification (logs outside the repository):

- 231 focused Content checks pass on the unmodified source revision:
  `C:/Users/mggar/Source/widow-final-two-independent-content.log`.
  Filter: `Counterattack|RapidResponse|BlackWidow|AbilityData|Preparation|SafeHouse`.
- All four additional form controls pass:
  `C:/Users/mggar/Source/widow-final-two-independent-forms.log`.
- The strengthened timing marker plus four form controls pass, 5/5:
  `C:/Users/mggar/Source/widow-final-two-independent-extra-controls.log`.
- All six actual-row durable replay cases pass (four Counterattack, two Rapid
  Response), including fresh-host reconstruction and strict JSON checks:
  `C:/Users/mggar/Source/widow-final-two-independent-server.log`.

The durable factories retain documented synthetic setup actions, plus a
simplified Counterattack Synth-Suit row or a Rapid Response entry choice.
Their 08030/08031 rows are canonical. They establish nonpublic durable
composition, not public Black Widow setup or whole-pack acceptance.

The source packet's compiled received-versus-dealt mutation evidence was
reviewed but not rerun here. No new production decision or mechanism change is
made. Full managed lanes, native UI, platform certification, public admission,
the other eleven-face packets and whole-pack acceptance remain integration
work; this verdict does not claim them.
