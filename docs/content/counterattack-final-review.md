# Counterattack timing review

Reviewed production revision `d0ef0685fb21e98b142ad5090c9592237934885e` after
the independent timing controls in `a4258184` and receipt packet `cc92090f`.
Current RR 1.8 attack procedure step 6(a)/(b) governs: forced damage responses
and retaliation precede optional Counterattack. The older ruling order is
superseded. Same-title promotion retains character identity under
`rr:villain-defeat.3.1`; different-title promotion does not.

Source review found no further blocking production issue in this packet.
Damage placements are retained on the exact `EnemyAttack` and copied to its
completed occurrence. Native retaliation and authored forced responses enter
the same forced-response tier. Target admission uses captured recipient facts;
received quantity additionally checks original actor, recipient controller and
incarnation. Successor selection checks captured configured stages and expected
entry incarnation. The standalone non-event discard-source payment rejection
is narrow: it excludes extra resource selections without changing the separate
event printed-price payment path.

Added four synthetic durable-factory cases covering take/decline and ordinary
versus same-title promoted attacker. Both canonical SessionSave and StoredSession
JSON round-trip byte-stably and reject unknown members. A fresh EngineHost
reconstructs the pending attack-end receipt from decisions, including original
attacker, recipient and controller, with equal digest, RNG state/word count and
prompt. Continuing produces the same semantic events and damage as uninterrupted
execution. Successful Counterattack is restarted again after its discard and
Preparation completion receipt, then Synth-Suit resolves identically. Stored
generation bytes are unchanged by each restart. The factory uses the complete
authored ability book with explicit test-local action, Counterattack and
Synth-Suit replacements; configured Rhino II and its reveal dependencies execute
their actual abilities. These tests do not admit Black Widow publicly.

Verification: four durable review cases pass in
`C:/Users/mggar/Source/counterattack-final-replay-complete.log`; 27 focused
Counterattack content cases pass in
`C:/Users/mggar/Source/counterattack-final-content.log`, including the original
independent retaliation and promotion controls. Initial failures were fixture
errors (DSL boolean spelling, duplicate stage and incomplete Core reveal
dependencies), corrected before this final run. No production code, version
identity or archive fixture is edited. Existing queued-attack and suspended
resolution controls were reviewed; no new nested-activation stress test, full
managed suite, native UI, public Black Widow admission or platform certification
is claimed here. Replay v27 integration and broader acceptance remain lead-owned.
