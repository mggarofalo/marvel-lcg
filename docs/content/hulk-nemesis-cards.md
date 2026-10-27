# Hulk obligation and nemesis cards — MARVEL-849

Implementation from base `b753c463`; ordinary research-card packet only.
The ability book appends exactly 10025–10028, reaching 716 rows and 31/33 Hulk
faces. All 712 predecessor raw UTF-8 objects are byte-for-byte preserved with
LF. Toe to Toe 10015, Beat Cop 10029 and unknown control 90001 remain absent.
No engine, setup, admission or starting-counter changes are included.

Independent actual-card review found that Clash's original character query also
included alter-egos. Its corrected row uses the existing hero-form/allies query.
See [the independent review](hulk-nemesis-cards-review.md) for the concrete source
failure, additional adversarial cases and final corrected-candidate evidence.

Authority is generated `datasets/cards/cards.json`, current RR 1.8 and local
rulings. Corrected Inner Demons removes Then (official errata in
`datasets/rulings/pages/official-ffg-rulings.html`); the historical All Tied Up
answer that depends on Then does not govern this corrected text. Its mandatory
form change precedes the resulting-form branch and normal obligation cleanup.
Abomination's printed-resource test excludes wild under `rr:wild-resource.3`.
Clash's exact ally target follows the vendored official answer: an undefended
selected ally takes the attack damage. `rr:first-player.1` governs both ties;
`rr:stun-stunned.7` and `rr:tough.2` distinguish attack replacement from prevention.

Actual-card tests use nonpublic prepared boards and full encounter reveal for
Inner Demons, Total Destruction and Clash. Abomination tests schedule the actual
attack procedure and drain its forced response. Cases cover hand shortages,
non-first-player obligation routing, physical/wild/mental icons, ally defense,
stun, per-player entry threat/hazard, Abomination in another play area, both
ranked ties decided by seat 1, modified ATK, selected allies and heroes, tough,
surge and treachery cleanup. Public Hulk gameplay remains untested and unadmitted.

Actual ordinary basic thwart excludes Total Destruction while Abomination is
present; the absent control removes one threat. Actual printed To the Rescue
has no legal action while this is the only threatened scheme and Abomination
is present; the same-cost absent control plays, pays and removes two threat.
These reachable controls demonstrate no engine prerequisite for the tested
paths. They do not claim general support for every projected conditional.

Evidence is under `C:/Users/mggar/Source/evidence/`:

- `hulk-nemesis-inventory-final.log`: 326 passed, including all 21 new actual
  card cases, every affected whole-book inventory owner (including unchanged
  owners), all Hulk tests, AbilityData, ranked/selected activation and citations.
  Explicit predecessor pack slices remain exact; the three customization rows
  now use their exact bounded slice rather than the remainder of the book.
- `hulk-nemesis-mutant-inner-flip.log`: removing mandatory flip kills four cases.
- `hulk-nemesis-mutant-abomination-wild.log`: counting wild kills the printed-wild control.
- `hulk-nemesis-mutant-abomination-damage.log`: reducing damage kills both physical controls.
- `hulk-nemesis-mutant-total-lock.log`: removing the prohibition kills both
  ordinary action exclusion controls and the live permission check.
- `hulk-nemesis-mutant-clash-surge.log`: removing fallback surge kills the stun case.
- `hulk-nemesis-restored-build.log`: restored nonincremental Release build,
  zero warnings/errors. `hulk-nemesis-restored-final.log` repeats the same 326 checks.

All mutants were serialized against the same actual-row tests and restored in
`finally`; data mutations affect only the four appended rows. The earlier
initial 16/17 run exposed and corrected an ignored row-level condition in Total
Destruction; its current conditional is inside the constant effect. A later
20/21 probe failure was an incorrect assertion demanding a playable event while
all threat was locked; the corrected test asserts exclusion plus its legal
absent control. The initial broad affected run was canceled to serialize these
probes and is excluded from acceptance evidence. No engine behavior was weakened.

Architecture: all 129 checks pass; log hulk-nemesis-architecture.log.

Normal complexity hook required extracting the defense-answer loop into the fixture; all 21 actual cases pass after this mechanical test-only change (hulk-nemesis-layout-final.log).
