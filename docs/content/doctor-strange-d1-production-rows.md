# Doctor Strange D1 production rows — MARVEL-808

Parent MARVEL-565; base `05cb51c2`, branch `feat/strange-d1-rows`.
This packet appends 09001a/b, 09002, 09005 and 09036 to the production ability
book, bringing it from 654 to 659 rows. A raw JSON object substring comparison
against the base confirms all 654 existing rows remain byte-identical. No engine,
public admission, setup or UI implementation changes are included.

Printed authority is the generated `datasets/cards/cards.json`: Spell Mastery
exhausts Doctor Strange and pays the Invocation cost; Natural Talent discards
once per phase; three-cost Wong exhausts before choosing controller-identity
healing or the singular Invocation deck top; one-cost Master additionally pays
the Invocation's printed cost; zero-cost Winds draws three and self-discards.
The complete `pack:mc08:the-invocation-deck` insert owns deck initialization and
the separate immediate reset without penalty. Master's intermediate reveal/reset
follows current reconciled `ruling:b07d53e12625ab23`, rather than its superseded
no-look answer. The reviewed engine packets are recorded in the linked Spell
Mastery, Master and top-discard evidence documents beside this one.

Ordinary SpellMastery and Master fixtures now consume the actual production
identity, Master and Winds rows. Their synthetic paid/choice Specials explicitly
replace the selected face; resource-window controls explicitly replace their
production programs. The replacement helper requires a unique original face and
does not relax catalog duplicate rejection. Top-discard tests consume the actual
production book, including the existing Make the Call transfer of owner-1 Wong
to controller 0, the printed three-resource payment, subsequent discard, and
separate controller-healing cases. Constant fixtures consume the actual identity
rows. Inventories include exactly these five additional faces; adjacent 09032,
09003 and 11028 remain genuinely unauthored negative controls.

## Evidence and limitation

Logs are outside the repository under `C:/Users/mggar/Source/strange-d1-*.log`.
The data/citation and new printed-inventory/form cases pass: 37 cases in
`strange-d1-data.log`. The characteristic classes cover owner/controller Wong,
Natural Talent phase reset and form changes, combined event/Invocation payment,
exhausted-hero Master, ordered physical discard/reveal/reset/return, Winds draw
and separate ordinary player-deck exhaustion penalty.

Five production-data mutants were parsed and lowered by the runner using the
current compiled test assembly (`--no-build`): omitted Spell Mastery exhaustion
fails 3 cases; Natural Talent limit two fails 1; omitted Wong exhaustion fails
4; omitted Master return fails 2; Winds draw two fails 3. Logs use suffixes
`mutant-spell-exhaust`, `mutant-natural-limit`, `mutant-wong-exhaust`,
`mutant-master-return` and `mutant-winds-draw`. These are lowered data mutations,
not compiled production-source mutations. Exact data bytes were restored and the
Content project rebuilt with `--no-incremental`: zero warnings/errors in
`strange-d1-restored-build.log`.

The retained `SpellMasteryTests.RejectionPreservesPromptCardsRngAndPendingWork`
`form` case directly calls `hero.TurnTo("09001b")` between obtaining a hero
offer and answering it. With both actual identity rows, the old action index now
resolves Natural Talent; its expected exception fails. The former fixture had no
alter-ego row. The final availability assertion now correctly excludes Spell
Mastery while allowing Natural Talent. This direct external world mutation is a
synthetic engine probe, not a demonstrated public session stale-decision defect.
`GamePrompts.Handle` reuses action ordinal/anchor handles;
`EngineHostResolution` rejects a stale expected session revision before resolving
an answer. The new ordinary ChangeForm test separately proves fresh alter-ego
offers exclude Master and include Natural Talent, then hero form offers Master.
The retained synthetic failure needs lead adjudication; this packet makes no
green-candidate or complete-pack acceptance claim.

`strange-d1-restored-tests.log` records whole affected SpellMastery, Master,
Invocation, DoctorStrange, AbilityData, citation and the four changed BlackWidow
inventory classes: 162 passed and the one direct-form-mutation case failed
(163 total). The initial TRX was moved outside the repository to
`C:/Users/mggar/Source/strange-d1-initial.trx`. Normal commit hooks remain enabled; their result is recorded
in `strange-d1-hooks.log`. Independent review and integrated wider gates remain
with the delivery lead.
