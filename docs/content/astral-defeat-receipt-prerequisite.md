# Astral Projection deferred defeat receipt prerequisite

This packet follows `3758d21f` and the public offer filtering in `9eb7d9e5`.
It implements the separate engine prerequisite identified by Absorbing Man's
public starter seed 5 after accepted decision 93, round 11: Astral Projection
(09003) may select Hydra Patrol (04154), physical object 89 with two threat.
The initial removal defeats that scheme. Its When Defeated search, engagement
and shuffle resolve before Astral continues.

`AbilityDefeatReceiptProjection` admits a `removeThreat` continuation only when
its recipient is the retained `chosen` binding, its amount is a numeric result
receipt, and that exact physical recipient is already in the projected departed
set. This proves that the tail needs no prediction of the opaque defeat window.
It does not grant another target or project encounter-deck contents. Other
targets, an unbound recipient, a live chosen recipient, and board-reading amounts
retain the existing unsupported boundary.

The runtime continuation keeps its immutable result memory and captured
incarnation through the existing `AbilityMemory`, `AbilityCardReference` and
continuation restoration. The engine does not reread the shuffled deck to obtain
the numeric inspection receipt, or apply a departed scheme's tail to a new copy.
No ability-book, Agenda, Runner, or client policy changes are included.

The authorities are printed 09003 and 04154 in `datasets/cards/cards.json`,
`rr:and.1` (independent effects are not dependent on each other),
`rr:when-defeated-abilities.2.1` (departure follows When Defeated resolution),
and `rr:target.4.1` (invalid targets are not resolved against).
The vendored MarvelCDB FAQ records both card codes as queried without entries.
The conservative projection boundary is an engine choice.

## Focused evidence

Eleven cases pass in `AstralDefeatReceiptTests` and
`DeferredDefeatReceiptBoundaryTests`:

- Actual production 09003 and 04154 resolve both independent effect orders with
  one and two players, including real minion searches, engagements and shuffles.
- Repeated action/description queries preserve card digest, complete RNG state,
  RNG words and pending agenda before the first query. Subsequent execution
  matches an unprobed control's complete event sequence and observed state.
- Two synthetic intervening departure/reentry cases preserve the new incarnation's
  six threat and leave another same-title scheme and the main scheme untouched.
- Four synthetic unsupported tails retain refusal: query recipient, board-reading
  amount, unbound `that`, and a live chosen recipient after another scheme's defeat.
- A synthetic enclosing action inspects Hydra Regular (04152), whose printed
  Boost attribute is two. Hydra Patrol puts that exact minion into play and
  shuffles the remaining one-icon pile. A later conditional draw still reads two
  from the retained inspection receipt; the current top has one icon.

A compiled mutant that restores refusal of the new receipt shape fails all six
actual-card/incarnation cases. Source is restored exactly and rebuilt without
incremental compilation. A separate broadening mutant that removes only the
departed-set condition survives the five synthetic boundary cases: other existing
opaque-continuation restrictions still refuse those unsupported enclosing shapes.
That survivor is preserved explicitly; it is not counted as a killed mutant or
as proof that the departed-set check is redundant outside admitted shapes.

Architecture passes 129 cases and rule citations pass two cases. A broad Content
selection reaches 785 cases: 772 pass and 13 Core transcript wrappers fail at the
same `card-actions.feature:331` cancel/status fixture. The parent integration has
the separate fixture correction `4b7d42cf` and independent review `732c`; this
packet neither changes that fixture nor claims those failures are green. Final
whole-suite integration remains the lead's gate.
The same fixture failure reproduces in all 24 selected transcript wrappers with
both runtime files restored to `3758d21f`. After exact source restoration and a
nonincremental rebuild, the affected selection excluding the Behavior namespace
passes all 770 cases (`verified-restored-affected.trx`).

## Complete public traces

The unchanged public-game policy and host/replay assertions pass both exact seeds.
Every accepted decision compares host event JSON, RNG words, state digest and
result. Strict durable JSON is then replayed through the production verifier.

| Scenario | Result | Rounds / decisions | RNG words | Final SHA-256 |
|---|---|---|---|---|
| Absorbing Man, starter seed 5 | VillainWins | 12 / 99 | 252 | `7AB90F5FF68B75151C3F4E9E2FE9F5A4355273DDF123EBF8FC9F547468D7F4E9` |
| Rhino Expert, starter seed 6 | PlayersLose | 3 / 27 | 169 | `551FD33F1573F50F60892671AB54204260CCB6FFF0AE06A24AAC76C4E85713AE` |

Trace SHA-256 values are respectively
`6ADADF866488A7A6B8EE565176688D1BBE5C0A9617EF586FF55B449AF5111470` and
`FE88375F96CE59E14A94C9B088A51D779691CF7203B8829661FA51EB04BEDFAA1`.
These are finite engine/host/replay results, not complete pack or UI acceptance.

Logs and TRX files are under
`C:/Users/mggar/Source/evidence/astral-defeat-receipt/`:
`focused.trx`, `public-seeds-5-6.trx`, `refusal-mutant-build.log`,
`refusal-mutant.trx`, `refusal-mutation-result.txt`, `mutant-build.log`,
`mutant.trx`, `mutation-result.txt`, `final-restored-build.log`,
`final-restored-affected.trx`, `architecture.log`, and `citations.log`.
The finite baseline check and restored affected selection are recorded in
`baseline-transcripts.trx`, `verified-restored-build.log` and
`verified-restored-affected.trx`.
Initial focused and public command logs are the sibling files
`astral-defeat-receipt-focused.log` and `astral-defeat-receipt-public.log`.
The original unsupported public prefix remains archived under
`evidence/astral-offer/absorbing-man-5-new-boundary.session.json`.

Independent review of the combined filter and prerequisite, final integration,
platform certification and native acceptance remain separate.
