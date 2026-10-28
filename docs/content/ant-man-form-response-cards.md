# Ant-Man form responses — MARVEL-569

Bounded research content packet on integration base
`9f93ee9d65c0201f9b2fde81941f9fcd445c266f`: Giant Strength `12009`, Moxie
`12016`, and Lay Down the Law `12031`. The ability book contains 745 authored
faces, including 22 Ant-Man faces. Its previous 742-row text prefix is preserved.
This packet adds no engine mechanism or public pack admission.

## Authority and interpretation

Printed authority is `datasets/cards/cards.json`. Current RR 1.8 governs:
`rr:lasting-effects.1`, `.5` and `.6` describe persistence, expiration and
permitted initiation periods; `rr:response.1` and `.2.1` govern control and
separate physical copies; `rr:resource.5` distinguishes generated excess;
`rr:wild-resource.1` governs declared payment types; and
`rr:confuse-confused.5` requires payment before confused cancels a thwart.

Vendored ruling `ruling:b777f490f7a0b34d` agrees with current RR 1.8: Giant
Strength has no effect during the villain phase, because no turn is being taken.
The earlier Moxie ruling `ruling:b3cc10c46c8d696b` distinguishes one response
from its lasting bonuses; current lasting-effect and response clauses agree.
No card-specific FAQ entry is present for these three queried faces in the
vendored MarvelCDB FAQ corpus.

Giant Strength responds only to its controller changing into Giant hero form.
Its +1 ATK expires at the current turn's end, including another player's turn
and an eliminated active player's elapsed turn. Moxie responds to either hero
destination and grants its controller's hero +1 THW, ATK and DEF through round
end. Lay Down the Law is a labeled hero thwart response with an ordinary scheme
choice; committed mental payment removes four threat, otherwise three.
Unallocated mental overpayment does not satisfy its condition.

## Behavioral evidence

`AntManFormResponseCardTests` exercises controller and destination restrictions,
separate copies, all three Moxie stats and its persistence beyond turn end,
mental/physical and declared-wild payment, generated but unallocated mental
excess, rejected payment with unchanged prompt/state/RNG/agenda and successful
retry, rejected target handle, and confusion's paid cancellation. These are
synthetic research boards using the production rows, not public setup games.

The 13-case `GiantStrengthPeriodTests` now selects the production Giant Strength
row. It retains normal/off-turn expiration, ending interrupts versus responses,
phase boundaries, villain-phase exclusion and elimination-window coverage.

The final focused command selects `AntMan`, `GiantStrengthPeriodTests`,
`AbilityDataTests`, `AbilityProgramTests` and `RuleCitationTests` in
`Marvel.Content.Tests`: **81 passed**. Raw log:
`C:/Users/mggar/Source/evidence/ant-form-response-final-focused.log`.
Six production DSL mutants lower successfully and fail distinguishing behavior:
Giant Strength's duration changed to round and Giant destination changed to Tiny;
Moxie's duration changed to turn and bonuses changed to two; Law's qualifying
payment changed to energy and mental removal reduced to three. Their raw logs
are `C:/Users/mggar/Source/evidence/ant-form-response-mutant-*.log`.
Removing Law's label is refused during DSL lowering and is not counted as a
behavioral mutant. The restored candidate passes the same 81 focused checks:
`C:/Users/mggar/Source/evidence/ant-form-response-after-mutants.log`.
Normal commit hooks remain enabled. Full pack gameplay, replay/publication,
public admission, native UI and cross-platform CI are outside this packet.
