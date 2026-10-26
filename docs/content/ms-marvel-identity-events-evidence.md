# Ms. Marvel identity and event research packet

Base: `df8661c05dd9a4b0061652d0fd1d0cab86a6c2ae`. Seven actual authored
faces: 05001a/b, 05003 Big Hands, 05004 Sneak By, 05005 Wiggle Room,
05010 Embiggen!, 05011 Shrink. Engine mechanisms, setup and public admission
are outside this packet. The complete 34-face pack remains separate.

Printed facts: `datasets/cards/cards.json`. Reviewed vendored rulings:
`ruling:7f69f006cddce07e` (completed event in discard),
`ruling:556ecad3abe55ce9` (qualified traits),
`ruling:b144656def387a1e` (same-copy off-turn Wiggle Room replay),
`ruling:88369b11361157e4` (printed cap). `faq:05010` modifies every damage
instance; `faq:05011` distinguishes removal from prevention. Current Rules
Reference defense semantics take precedence over the historical FAQ 05005
claim that defense events do not establish defense.

Actual-row evidence covers normal paid Big Hands with Embiggen! and Sneak By
with Shrink, accept/decline and ineligible trait/controller/exhaustion;
exact-copy recovery and decline after actual attack resolution;
removed/foreign/exhausted/unqualified/unplayed event negatives; real damage
window Wiggle Room prevention/draw, recovery and same-copy replay; Teen Spirit
ordered exact-card recovery, exhaustion/no-match boundary, query purity,
form/readiness and once-per-round reset. Existing mechanism fixtures remain
separate and are not printed-text authority.

Nine temporary data mutants were loaded successfully and rejected by focused
actual-row tests: omit Morphogenetics exhaustion; replace its qualified traits;
omit Teen Spirit limit; change its identity set; reduce Big Hands damage;
reduce Sneak By removal; reduce Wiggle Room prevention; reduce Embiggen! bonus;
reduce Shrink bonus. Each mutant produced behavioral assertion failures, with
logs under `%LOCALAPPDATA%/Temp/msm-mutant-*.log`. Exact dataset bytes restored
before the final green run. No engine mutant or whole-pack acceptance claim.

Focused verification command:

```powershell
dotnet test tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release --filter 'FullyQualifiedName~MsMarvelEventCardTests|FullyQualifiedName~MsMarvelIdentityEventTests|FullyQualifiedName~MsMarvelTeenSpiritTests|FullyQualifiedName~AbilityData|FullyQualifiedName~AbilityProgramTests'
```

The final run and hook result are recorded in the delivery handoff. Broader
integration lanes, independent review, each-instance/cap regression integration,
remaining 27 faces and public setup/save/native journeys remain with the lead.


## Independent review of 7399cff33fa0322660cfa2a7563d73b801e5229b

Reviewed in isolated detached worktree `marvel-lcg-msm-identity-review` against
`df8661c0`. Raw printed rows, the four named vendored rulings, FAQ 05010/05011,
and Rules Reference event/player-deck/response clauses were inspected. The
unknown-card control still uses unauthored Goblin Reinforcements (02026), and
admission inventory permits only these seven Ms. Marvel faces.

The author's focused command passed 64/64 independently. Retained review tests
add 20 cases using the unmodified candidate upgrade/identity rows:
`MsMarvelShrinkReviewTests`, `MsMarvelPaymentReviewTests`, and
`MsMarvelEmbiggenReviewTests`. Shrink cases bind its actual row into existing
cap, prevention, active-body, uncertain preflight, and settled-payment fixtures.
The Embiggen test uses a test-only Melee row with its printed 3+3 quantities
and two fixed legal recipients; it proves both instances become 5 and does not
claim Melee target-choice admission. The recovery test gives both a printed
Attack-trait card and a non-trait card the same executable attack body, proving
the trait distinction with a positive control. A first direct-execution version
of that fixture omitted the ordinary CardPlayed occurrence; it was corrected to
use Game.Resolve before counting its results.

All nine independent temporary data mutants loaded and produced behavioral
assertion failures: omitted Morphogenetics exhaust, omitted trait condition,
omitted Teen Spirit round limit, wrong identity set, and decremented Big Hands,
Sneak By, Wiggle Room, Embiggen and Shrink amounts. This rerun uses the author
card tests plus the retained review tests (46 cases per mutant), not merely
parser rejection. Dataset bytes were restored exactly (SHA-256
`d14e45b66d2fe286d4cae60263f2859321ae220497f8a4ac564029a103b9c3a7`).
Logs: `%TEMP%/msm-independent-mutant-*.log`; summary and repeatable temporary
mutation script: `%TEMP%/msm-independent-mutants-summary.json` and
`%TEMP%/msm-independent-mutants.py`.

Final restored command extends the author's filter with all three review class
names. Its log is `%TEMP%/msm-independent-final.log`. Normal pre-commit group
results are in `%TEMP%/msm-independent-hooks.log`. This independent review is
limited to the seven-row packet and its prerequisite behavior; remaining 27
faces, public setup/save/native journeys and whole-pack admission remain future
work. No production files were edited by the reviewer.
