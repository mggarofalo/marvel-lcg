# Black Widow support and encounter batch

Base: `9880d167` (f4105b82 plus reviewed preparation authoring/review).
Four actual rows append 08023 Quincarrier, 08025 Burn Notice, 08026 Taskmaster,
and 08029 Deadly Shot. There are 632 total rows and 21/34 Black Widow faces.
The 628-row raw prefix is 625672 bytes, SHA-256
`fa5d7e863d369207f017fbc98b5c3405c66b551c0435cb3750539d7fdb3810c3`.
No starting counters, engine, setup, admission or printed-data edits.

Authorities: raw `datasets/cards/cards.json` for every assigned face; current
RR 1.8 obligation, choose-option, first-player, scheme-enemy-activation,
you-your, leaves-play and resource-ability. Local rulings were searched for each
card. Quake ruling `2965145028810de5` confirms zero SCH still schemes; it is
consistent with current activation procedure. Taskmaster rulings
`c46741ceaa9f5e18` (reminder stars) and `ead76dd0b3c966ee` (same-name villain
and Shadows of the Past) do not replace its printed count or boost text.
No relevant local Burn Notice, Quincarrier or Deadly Shot clarification was found.

First test-local characteristic compositions passed 17 cases before authoring.
Actual-row tests exercise both forms and nonfirst seat, ordinary full encounter
reveal/cleanup, obligation recipient transfer/optional flip/exhaust/removal and
surge dealing, tied highest own Preparation selection with first-player chooser,
foreign/ordinary/lower-cost exclusion and invalid retry; live engaged-seat
Taskmaster census, attack/scheme boost resolving seat and expiry; Quincarrier's
identity Avenger restriction with an Avenger ally control and unrestricted wild
payment in both forms; Deadly Shot's own upgrade selection, invalid input and
nondependent damage/threat when no upgrade exists. The prepared boards are
nonpublic fixtures; they make no complete pack/setup journey claim.

The existing PreparationPrintedFixture explicitly replaces the actual 08023 row
with its labelled synthetic defeat-suspension observer. It must not append a
second row with the same face. Existing actual preparation rows retain their
semantics and the historical batch inventory retains its original ordinal range.

Quake remains unauthored. Its zero-SCH/either-engaged-seat, villain exclusion and
exhaustion probes passed, but a compiled incarnation probe failed: schedule a
zero-SCH Taskmaster scheme; open Quake's response; explicitly discard and reenter
that physical minion before answering; Quake deals 2 to the new incarnation.
RR `rr:leaves-play.1` says it is a new copy. The minimal failing method is
`QuakeDoesNotDamageAReenteredSchemeActor` preserved externally in
`C:/Users/mggar/Source/bw-quake-compiled-red-tests.cs`; the test-local Quake row
is in `bw-next-rows.json`, and the compiled failure is `bw-next-extra.log`.
This external movement is an isolated retained-occurrence challenge, not a
claimed ordinary game sequence. A separate engine implementation/review packet
must bind the completed scheme actor's incarnation before 08012 admission.

The actual-row mutant changes both Deadly Shot continuation amounts from 1 to 2.
Its compiled tests must fail on the exact damage/threat result. Restoration copies
the original book back and forces a nonincremental Release build before the final
focused run. Logs under `C:/Users/mggar/Source/`:
`bw-next-probe2.log`, `bw-next-actual.log`, `bw-next-extra.log`,
`bw-next-mutant.log`, `bw-next-restored-build.log`, `bw-next-restored-tests.log`.
Independent source review and combined integrated gates remain with the lead.

Quake gap scope: this red directly mutates the pending world, rather than moving
the minion through an ordinary nested response. `PhaseStep.SchemeThreat` creates
both the threat actor and occurrence subject from the scheming minion ObjectId;
the target is the main scheme. The probe retains that physical ObjectId through
Discard.Card then World.MoveToTop reentry. It observed damage 2 on the reentered
copy; it did not record numerical incarnation or ActorFacts snapshot values.
The separate pair must capture those facts, reproduce departure through actual
response ordering, and distinguish rejecting before exhaust from an already
accepted continuation losing its prior target. This packet establishes neither
that valid-flow reproduction nor a particular engine remedy.

Final evidence: the compiled Deadly Shot mutant failed all four form/upgrade
cases with expected 1, actual 2. Restored nonincremental Release build passed
with zero warnings/errors. The final restored focused filter passed all 144
cases, including ability-data, printed preparation regressions and rule-citation
checks. No full Content/broader suite ran in this packet. Normal commit hooks
remain enabled.
