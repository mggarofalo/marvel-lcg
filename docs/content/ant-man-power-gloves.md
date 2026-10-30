# Power Gloves actual-row packet (MARVEL-569 / MARVEL-872)

Base: `b10f3ec533207725b48bf47b75daa0518110e563`. This packet appends actual
research row 12017 after the 732-row book containing Call for Aid and independently
reviewed ally completion source `7ba814c24489cf87ea17f2f86452735368f56e0a`.

## Authority and scope

Printed authority is `datasets/cards/cards.json` 12017: attach to an Avenger
ally, maximum one per ally; optional Response after that ally attacks or thwarts,
dealing one damage to an enemy. The local FAQ lists 12017 without an answer;
vendored rulings contain no specific Power Gloves ruling. RR 1.8 attach-to.3,
max-maximum.4, response.1, ownership-and-control.2.1, thwart.2 and
consequential-damage.1 govern. Trigger/query names are engine choices.

The ability book contains 733 authored faces, ten of 35 Ant-Man faces. No
starting counters, engine changes, setup, public admission, native interaction
or complete-pack acceptance is included. Replay compatibility remains MARVEL-870.

## Actual-row and synthetic evidence

`PowerGlovesActualTests` uses the actual book for paid cross-player attachment,
Avenger-only and maximum-one restrictions with eligible controls, attack/thwart
accept/decline, controller 1 versus owner 0, enemy target eligibility and lethal
consequential damage after the response. Basic powers use printed Vision stats.
`AllyThwartCompletionTests` now retains actual Power Gloves in ordinary cases
while its Vision Action is explicitly synthetic. These tests cover labeled
thwart, exact attached performer, blank/departed sources, confused cancellation,
response ordering and host incarnation. Only its nested-response variant mutates
the actual response to insert a synthetic choice. `IndependentAllyPowerFixture`
explicitly replaces actual 12017 with its documented synthetic response body;
its nested labeled powers remain mechanism evidence rather than printed behavior.

## Verification

The nonincremental Release build completed with zero warnings/errors. The complete
actual-row class, completion and independent-review classes, basic response,
continuation, controller, attack-binding, basic-use, ability-data and rule-citation
filter passed 111 cases. Logs are outside the worktree under
`C:/Users/mggar/Source/ant-man-power-gloves-`: `build.log`, `focused.log`.

A compiled actual-book mutant changed both 12017 damage amounts from one to two.
Three of six actual-row tests failed at the intended damage assertions: accepted
basic attack, accepted basic thwart and minion target. The row was restored in
`finally`; a fresh nonincremental build again had zero warnings/errors, and all
111 cases passed. Logs: `mutant-build.log`, `mutant.log`, `restored-build.log`,
`restored-focused.log`. The initial test preparation used an invalid zone enum;
it was corrected before this passing verification and is not an engine finding.

The raw audit compares with the exact base: all 732 predecessor object bytes and
LF are preserved, 733 IDs are unique, and ten of the printed 35 Ant-Man faces are
present. Restored book SHA256: `ba342d8fa6f02d1a4cba2ef6b56cfb3fa4b9a80208dcc10abee4f826a7553a65`.
Normal pre-commit hooks are required on this packet's source commit.
