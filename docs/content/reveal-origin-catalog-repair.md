# Reveal-origin catalog repair

This bounded repair follows the independent review of the authority catalog at
`530f156e`. It preserves all other adjudications, generated printed card text,
and production ability rows. No engine or transcript runner changes are included.

The reviewed sources are the complete pinned answers in
`datasets/rulings/rulings.json` and the current Rules Reference clauses:

- `ruling:0a7e7b33c1e95fa3`: simultaneity maps only to `rr:cost.5`.
  Resource division has its own `outside-core` branch against `rr:cost.5.1`:
  no legal Core card combines a positive resource cost with a positive resource
  ability cost. The chosen ally's aspect for a Power Of resource card maps
  separately to `behavior:faq:01071:power-of-aspect-pays-printed-ally-cost`.
  Echo remains outside Core.
- `ruling:6b756402a689cade`: the existing 01004 cancellation obligation now
  binds a legal Expert Rhino transcript using actual Enhanced Spider-Sense and
  Exhaustion. Spider-Man stays ready, the event and Energy are discarded, and
  Exhaustion reaches the encounter discard pile. Expert setup's Breakin' & Takin'
  hazard independently deals Sandman; Hydra Mercenary stays in the encounter
  deck, distinguishing cancellation of Surge from the mandatory hazard deal.
- `ruling:9196106259a1f1a4`: exactly defending requires the defender's modified
  DEF to equal the attacker's modified ATK for that attack. Damage prevention
  and reduction cannot satisfy that requirement. The branch remains outside Core.

The generated catalog retains 2,667 sources and now has 4,434 obligations.
The executable Core corpus contains 685 scenarios (684 preceding this repair).
The 01004 obligation also retains its existing Advance cancellation transcript.

## Local verification

Commands completed in the supplied Windows worktree with PTY execution:

- `dotnet run --project tools/Marvel.Behavior.Index -- write`, then `check`:
  catalog and generated Core face specification current.
- Nonincremental Release build of `Marvel.Content.Tests`: zero warnings/errors.
- Content selection `Behavior|GherkinFormatTests|RuleCitationTests`: 134 passed,
  including the complete admitted Core corpus, the quarantine rejection, and
  the new standalone cancellation transcript check.
- Rules selection `BehaviorAuthorityTests|GherkinFormatTests|RuleCitationTests`:
  25 passed, including the complete BehaviorAuthority class.
- Core selection `GherkinFormatTests|RuleCitationTests`: 7 passed.
- `git diff --check`: passed.

Full logs live outside the repository under
`C:/Users/mggar/AppData/Local/Temp/reveal-catalog-` with suffixes `write.log`,
`check.log`, `mutant-build.log`, `mutant-test.log`, `restored-build.log`,
`restored-content.log`, `authority.log`, and `format.log`.

## Mutation evidence

A temporary engine mutation schedules keyword Surge even when cancellation is
accepted, while leaving the printed When Revealed effect cancelled. The mutant
compiled nonincrementally with zero warnings/errors. The new standalone test
failed at `treachery-cancellation.feature:37`: Hydra Mercenary no longer occupied
the encounter deck. The failure's event trace includes a Surge deal, so this is
a behavioral kill of the intended distinction.

`EncounterRevealProcedure.cs` was restored byte-for-byte before the final
nonincremental build and passing Content checks. Restored source SHA-256:
`4c71988ef9968947aa35ba813bd498b4d7ec5aec61814a7735bd1af9f0f95e39`.
No production engine mutation is committed. An initial fixture attempt omitted
Expert Rhino's hazard deal and was corrected; it was not an engine defect.
A preliminary mutation invocation aborted before applying any mutation because
of a Python decoding error; only the subsequent compiled, killed mutant above
is claimed as mutation evidence.

Independent re-review, complete managed/integration acceptance, native Godot
journeys, and Linux execution remain integration scope. These focused local
checks do not claim cross-platform or product acceptance.

## Independent recheck and integration

Independent `reveal_catalog_recheck` accepts source `10c8cd84`. It compared all
2,654 pre-refresh source adjudications: only the intentional 01004 transcript
and mutation rationale change; exactly thirteen sources are added. The final
catalog has 2,667 sources, 4,434 obligations and 685 executable Core scenarios.
It inspected the compiled mutation trace and restored engine hash. Offline index,
25 Rules authority/citation/format, 134 Behavioral Content and seven Core format/
citation checks pass. Review tree remained unchanged; no extra source commit.
Review logs: `%LOCALAPPDATA%/Temp/reveal-catalog-recheck-{index,authority,content,format}.log`.

Combined with the 675-row Strange candidate and reviewed engine continuations,
offline index check and all 134 Behavioral Content/citation cases pass again.
Logs: `Source/evidence/strange-catalog-final-{index,content}.log`. Native,
second-platform and complete-pack certification remain separate integration gates.