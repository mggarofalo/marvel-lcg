# Ms. Marvel Bruno and conditional status research faces

Candidate: `feat/ms-bruno-status-reviewed`, based on `60273103` plus reviewed
four-face data commit `f789c1f5` (cherry-picked as `7e862df6`). This packet authors
only 05007 Bruno Carrelli, 05015 Tackle and 05031 Concussive Blow. The resulting
book has 565 distinct faces, including 23 of the 34 Ms. Marvel faces. The original
UTF-8 ability-book prefix through its last existing row is byte-identical.
Public Ms. Marvel admission remains excluded. No engine, UI or generated data
changes belong to this packet.

Authority is the joined printed text in `datasets/cards/cards.json`, current
Rules Reference v1.8 and vendored rulings. Bruno's two printed abilities remain
separate: alter-ego storage exhausts him and selects exactly one card from the
controller's hand, facedown and out of play; his Action exhausts him and selects
zero through three stored cards in either form. `rr:in-play-and-out-of-play.10`
and `ruling:6768213a7e0f4606` establish out-of-play storage. Host departure uses
ordinary attachment cleanup to the physical owners' discard piles. Accepting an
empty retrieval group retains exhaustion already paid.

Tackle stuns and Concussive Blow confuses the selected enemy; a committed physical
resource enables three damage. The conditional body retains attack targeting and
consequences. Current `rr:target.3.4` permits a target if at least one effect can
affect it. This governs already-statused and Stalwart targets with effective
physical payment. Earlier mutually inconsistent Tackle rulings do not override
that current published clause. Nonphysical payment needs an enemy that can
receive the status. The queried vendored MarvelCDB FAQ has no entry for these
three faces.

`MsMarvelBrunoCardTests` and `MsMarvelStatusCardTests` use the unmodified complete
authored book in prepared nonpublic games. They cover own-hand exact storage,
foreign-card refusal, form/readiness restrictions, bounded retrieval in both
forms, zero/one/two/three selections, rejecting four cards atomically, empty-group
cost commitment, face visibility, ownership and host-departure cleanup. Event
checks cover both status types, wild declarations, existing statuses, mental-only
admission and source-bound Biokinetic Polymer Suit payment against a physically
placed Kree Lieutenant's printed Guard/Stalwart. That fixture exercises printed
keywords only; it claims no Galaxy's Most Wanted gameplay admission. The
mental-only fixture explicitly removes the initial turn's extra draw after
`Playing` to preserve its intended payment pool.

The preexisting conditional-status tests retain their synthetic alternate branch
shapes by replacing the target face in their test-local book before appending
that synthetic interpretation. These tests do not substitute for actual-card
coverage. Shared bounded retrieval tests remain unchanged.

Verification on this packet:

- Focused Release content suite: 61 passed, zero failed. Filter includes the two
  actual-card classes, AbilityDataTests, AbilityProgramTests,
  ConditionalStatusPaymentAdmissionTests, BoundedCardRetrievalTests and
  BoundedRetrievalAdmissionReviewTests.
- Three valid authored-data mutants lower and execute, then fail distinguishing
  assertions: Bruno maximum three to two; Tackle physical kicker to mental;
  Concussive Blow damage three to two. No parse or compile failure is counted
  as a kill. Each mutation is restored from exact saved bytes.
- A non-incremental restored Release build precedes the restored 61-test run.
- Logs are outside the repository under `C:/Users/mggar/Source/`:
  `ms-bruno-status-final.log`, `ms-bruno-status-mutant-bruno-max-two.log`,
  `ms-bruno-status-mutant-tackle-mental-kicker.log`,
  `ms-bruno-status-mutant-concussive-two-damage.log`,
  `ms-bruno-status-restored-build.log`, `ms-bruno-status-restored-tests.log`,
  and `ms-bruno-status-commit.log`.

Independent card review and native grouped-interaction evidence follow in
separate assignments. This packet makes no broad-suite, public-pack or native
acceptance claim. No new unsupported whole-face gap was found.

## Independent actual-card review

Reviewed candidate `b5f9fb2e92ca2b03f1255be91e62148d4cac0ae6` against the three
joined printed faces, RR1.8 target, stun, ownership and Retaliate clauses, and
vendored Bruno/Tackle rulings. Both events unconditionally apply their status;
their three damage requires physical payment. RR1.8 `rr:target.3.4` governs
effective damage against already-statused or Stalwart enemies; the contradictory
older Tackle ruling does not override the current rulebook. The candidate diff
only appends 188 ability-book lines, preserving prior rows; independent inventory
reconciliation confirms 565 distinct faces and 23 Ms. Marvel faces.

`MsMarvelBrunoStatusIndependentTests` adds six actual-row checks. Bruno's
non-first controller stores from their hand even when the physical owner is seat
zero, and retrieves a foreign-owned stored card into the resolving controller's
hand without changing its owner. Both suspended choices identify seat one.
Status-only Tackle and Concussive Blow cause Retaliate despite dealing no damage;
a stunned attacking hero instead spends the payment and loses stun without
applying either the status or Retaliate. The existing actual-card and bounded
retrieval checks cover both forms, zero through three selections, paid empty
selections, malformed groups, facedown/out-of-play storage and departure cleanup.

The independent compiled negative control replaces the retrieval destination
with seat zero in `AbilityGroupRetrieval.Commit`. Five independent cases pass;
the non-first retrieval case fails its exact destination assertion. The source
is restored from saved bytes, verified unchanged, and rebuilt non-incrementally.
The final focused Release run passes 69 checks, including the original 61,
the six independent cases and two rule-citation checks. Logs outside the repo:
`C:/Users/mggar/Source/ms-bruno-independent-mutant-seat-zero.log`,
`ms-bruno-independent-restored-build.log` and
`ms-bruno-independent-restored-tests.log`.

Verdict: accepted for these three ordinary-action research rows and their engine
behavior. No ability-book correction or additional whole-face prerequisite was
found. This review does not establish public Ms. Marvel admission, native UI
privacy/presentation acceptance, PrepareWindow integration or broad regression.
