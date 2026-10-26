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
