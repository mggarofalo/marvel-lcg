# Yellowjacket and Size Increase

Candidate base: `4cf4f030d65b83d8470e986c5e55c0e23b2c654e`. The two actual
rows pass the source checks below; independent review remains required.
They do not admit Ant-Man.

The printed authority is `datasets/cards/cards.json`, faces `12027` and `12028`.
Yellowjacket's constants read the currently engaged player's hero form and
traits: Giant grants Giant and retaliate 1; Tiny grants Tiny and +1 ATK.
Changing engagement must not keep the previous player's form bonuses. Size
Increase prefers Yellowjacket, otherwise attaches to the villain; its printed
ATK/SCH modifiers already supply +2 and are not duplicated in the row. Uses
initializes three size counters and discards the attachment after the last use.

RR 1.8 `rr:activation` defines attacks and schemes as activations. The row
responds in their attack-end and scheme-end windows. Status replacement is not
a completed activation (`rr:stun-stunned.7`, `rr:confuse-confused.7`). The earlier
test-only diagnostic `b453bfcb` passed 14 cases for this composition and retained
two failures for a different, rejected attachment completion-event encoding.
That deliberately red diagnostic is not included in this source candidate.

The actual-row tests retain those successful composition cases with two distinct
legal identities in the engagement fixture. Additional tests compare
both ordering choices with Absorbing Man's printed after-activation response;
compare Size Increase with Captain America's Shield retaliation that defeats
Yellowjacket and removes the attachment; and observe a queued scheme after an
attack. The nested Rhino ability is explicitly synthetic; the two nemesis rows
and the competing printed abilities use the production book.

The book has 752 rows and 29 authored Ant-Man faces in this candidate only.
All prior 750 objects and their raw byte prefix are preserved; exact prefix
length and SHA-256 are in the external `ant-nemesis-book-prefix.json` evidence.
The authored-face inventories include the two additions, while Pym Particles
remains an unauthored negative control. Full pack setup and public admission
remain separate.

Windows Release source verification uses `MSBUILDDISABLENODEREUSE=1` and `-m:1`.
The first build exposed a test-fixture field/namespace name collision, which was
renamed. The next run passed 58 cases and failed the explicit counter inventory:
Size Increase's new pool was missing from that assertion. Adding its printed
three size uses preserves the exact inventory gate. After a nonincremental
restored build, all 59 selected behavior/data cases pass; catalog and incomplete
product admission checks pass 14 cases. Logs are
`ant-nemesis-cards-{focused,focused2,restored-build,restored-tests,catalog}.log`
under `C:/Users/mggar/Source/evidence`.

Two valid runtime JSON mutations distinguish the rows: increasing Yellowjacket's
granted retaliation fails its live-profile case while both placement controls
pass; removing two size counters after an attack fails the completed-attack case
while the scheme and two status-cancellation cases pass. These are interpreted
card-data mutations, not compiled engine mutations. Both restore the exact
candidate bytes in a `finally` block. Logs are `ant-nemesis-yellowjacket-mutant.log`
and `ant-nemesis-size-mutant.log`; the restored 59-case run covers both checks.
Normal hooks and independent review remain required. The inherited Power
regressions at this base are separately tracked by MARVEL-878.
