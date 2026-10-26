# Thor Hammer Throw stage continuation evidence

This packet addresses MARVEL-759: actual Hammer Throw (06005) lethally damages
Rhino I (01094), Rhino II (01095) reveals and searches for Breakin' & Takin'
(01107), then the same exhausted Mjolnir (06009) returns to its owner's hand.
The normal damage/reveal procedures own the search, shuffle, additional threat,
responses and departure. Admission only proves this continuation cannot require
an unprojected source or health read; it does not inspect hidden piles or predict
search success. The definition-based proof applies without printed-id allowlists.

Authority: printed joined 06005/01095/01107, rr:and (effects resolve
simultaneously), rr:villain-defeat (reveal next stage), rr:reveal.step.3
(resolve When Revealed). Rhino II has the search; Rhino III has toughness.

Validation on the source candidate above public-test commit a03df7b0:

- Six focused stage tests prove the actual searched scheme's three threat,
  no excess damage on the new villain, exact payment/exhaustion/return, absent
  search target, repeated-query RNG/pending-work purity and equal unprobed outcome.
  Synthetic source-changing stage/searched-card effects, damage effects and
  scheme-dependent health constants refuse before damage or RNG mutation.
- The original public test sources and policy run unchanged: 10/10 pass,
  including seed 8 decision 15 round 2 and seed 1 decision 28 round 4.
  Log: C:/Users/mggar/Source/hammer-stage-public-rerun.log. These are new-engine
  reruns; the historical red evidence remains unchanged. No player victory claim.
- Compiled mutant removing the new dependency check kills both actual-stage
  present/absent cases with the original unprojected When Revealed exception.
  Log: C:/Users/mggar/Source/hammer-stage-mutant.log. Restored --no-incremental
  build then 35/35 Hammer Throw/recovery tests pass; final focused run also 35/35.
- Broader 104-case Hammer Throw/PowerTrace run: 103 pass; one existing step-6
  diagnostic assertion also fails with the stage fix removed. It expects
  `step-6 interrupt` and receives `unprojected defeat window`. No assertion changed.

Scope: single-villain stage entry and a unique controlled titled upgrade return.
The proof admits self status, fixed self threat and recursive named side-scheme
searches with stable definitions. It rejects source-changing entries, character
entry/minion searches, conditional health and retargeted health constants,
reveal/entry/threat observers and later board-dependent damage. Klaw/Ultron and
broader RoRS stage continuation combinations remain unverified by this packet;
this is not certification of every admitted stage continuation.
