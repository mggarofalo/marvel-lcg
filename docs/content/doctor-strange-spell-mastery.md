# Spell Mastery D1a checkpoint — MARVEL-797

Base: `1b5fa2e9`; worker branch `feat/doctor-strange-spell-mastery`.
This is a nonpublic engine packet. No production ability rows, pack admission,
setup recipes or Godot interaction are added. Master remains MARVEL-798.

The test-local actual `09001a` Spell Mastery offers the exact public Invocation
top as its one required physical target. `invocationTop: "you"` prices its fixed
cost as an ability resource payment, preserving existing payment and exhaustion
procedures. Execution captures object, incarnation, owner and cost before any
payment. `resolveInvocation: "you"` validates that capture and schedules the
existing Special procedure, with no Wakanda Forever final-step bonus.
`discardInvocation: "this"` routes actual `09036` Winds through the typed
Invocation lifecycle after drawing three player cards. These DSL spellings are
engine choices; printed behavior remains the generated catalog and complete
`pack:mc08:the-invocation-deck`.

Fifteen focused cases pass: actual zero-cost Winds exhausts, draws three, discards
and exposes the next top; a last-card fixture recycles all five; the second seat
owns resolution; deeper/foreign/stale targets, wrong form and exhausted identity
reject without changing prompt/digest/RNG/pending agenda; repeated queries leave
identical subsequent events and state versus an unprobed control. Fixtures use
the real catalog and a test-local book, with all five physical Invocations.

The paid controls use actual two-cost `09032` with an explicitly synthetic
draw-three/discard Special; they do not change actual Winds' printed zero cost
or claim Crimson Bands' printed effect. Insufficient payment preserves the
prompt, card digest, RNG and pending agenda before a valid same-cost payment
completes. A repeated completed answer is rejected without further changes.
A live synthetic generator pays by defeating itself and opens an optional
interrupt before payment resumes; taking that interrupt draws once, then the
captured Special draws three and discards once for the second seat. Changed
owner, incarnation and top are rejected across that suspension before any
Special draw/discard. Already committed generator costs are not rolled back.
The synthetic Special would draw nine under the Wakanda final-step flag;
its observed draw of three distinguishes the Invocation scheduler context.

An unauthored actual Crimson Bands Special raises explicitly at Special
execution after the two-resource payment and hero exhaustion have committed.
The physical Invocation remains the public top; no substitute Special effect
or self-discard is fabricated. This is an unsupported execution boundary,
not an affordability rejection or transaction rollback.

A compiled mutant suppressing the Invocation self-discard was killed by all
three actual completion cases. Restored sources received a non-incremental
Release rebuild. Raw evidence is outside the repository:
`C:/Users/mggar/Source/strange-spell-{tests,mutant,restored-build,content}.log`.

Two additional compiled mutants were killed: suppressing the captured
incarnation comparison fails the changed-incarnation control; pricing every
Invocation as zero fails the explicit two-cost offer assertion. Restored
sources received a non-incremental Release rebuild and all fifteen focused
cases passed. Logs are `C:/Users/mggar/Source/strange-spell-capture-mutant.log`,
`strange-spell-price-mutant.log`, `strange-spell-controls-restored-build.log`
and `strange-spell-controls-restored-tests.log` in the same directory.

The compiled final-step mutant propagates Wakanda's final flag to an Invocation
and is killed by the synthetic paid completion controls. Final restored sources
received another non-incremental Release build and all fifteen focused cases
passed (`strange-spell-final-step-mutant.log`,
`strange-spell-final-restored-build.log`, `strange-spell-final-restored-tests.log`).
The full Content lane passed 6,588/6,588 in 5m33 before the final synthetic
final-step fixture strengthening, with identical production sources
(`strange-spell-controls-content.log`). This earlier broad run is regression
evidence; the final fixture has focused verification. Normal pre-commit
architecture, complexity and file-length gates run on the packet commit.

Independent engine review and integrated broader checks remain
before packet acceptance. Passing this nonpublic packet does not prove every
Invocation, Master, production ability rows, admission, restart, UI or complete D1.
