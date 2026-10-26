# Independent Defender and Family Feud review

Reviewed candidate `f2715ce316ed2f882f58d028a15d2c8a9d8ef91d` on
`review/thor-defender-family`, isolated from implementation in
`C:/Users/mggar/Source/marvel-thor-defender-family-review`.
Only actual full-book rows `06003` and `06027` are in scope. The book contains
591 authored faces, including 15 Thor faces. This review adds tests and this
record; it changes no engine, ability row, public admission or setup recipe.

Authority review used joined `datasets/cards/cards.json`, the current vendored
Defender clarifications `ruling:6e301a347a5392fb` and
`ruling:a1f4982cfebcd768`, `rr:cost-arrow-icon.1`, and the in-play and trait
clauses cited on the tests. The actual Defender row separates the entire
discard/engage payment from its chosen-scheme thwart. Family Feud leaves its
printed starting two threat to encounter reveal and adds the global Asgard
census once when revealed.

Nine independent cases in `ThorDefenderIndependentReviewTests` and
`ThorFamilyFeudIndependentReviewTests` challenge:

- An exhausted search with a threatened side scheme cannot remove Confused or
  threat; the event discards and the deck resets with acceleration.
- A minion discarded as the last encounter card remains the captured physical
  card after the immediate reset, engages, and pays for exactly three removal.
- Actual Quickstrike resolves before Thor's optional response, and declining
  that response precedes the threat choice without drawing cards.
- Thor in the second seat acquires Patrol during payment; only the side scheme
  remains selectable, and payment/discard ownership stays at that seat.
- Both Thor faces count identities, foreign controlled/owned cards, unhosted
  upgrades and Loki globally, with printed starting threat added exactly once.
- Re-revealing the same physical Family Feud after an Asgard card leaves play
  uses a fresh census and fresh printed starting threat.
- Effective granted traits contribute; exhausted, blanked Loki retains Asgard.
- Hosted Mjolnir contributes once while hand, deck, discard, set-aside, removed,
  encounter/dealt and beneath-card storage contributes nothing.

These are prepared research engine scenes, not complete deck-construction or
public setup journeys. The hosted-Mjolnir-on-Lady-Sif fixture is explicitly
synthetic topology, as are duplicate out-of-play cards and the lasting trait
grant. Controller/owner arrangements test global selector scope; they do not
establish a supported control-transfer action. The ordinary census control uses
unhosted Mjolnir. No product or native UI readiness is inferred.

Evidence is outside the repository at
`C:/Users/mggar/Source/thor-defender-family-review-evidence`:

| Artifact | Observation |
|---|---|
| `tool-restore.log`, `husky-install.log` | Normal Husky tool restoration and hook installation. |
| `initial.log` | Initial nine independent cases pass; superseded by final combined run. |
| `defender-two-threat.log` | Actual row changed from three removal to two: three of nine independent cases fail. |
| `defender-no-cost.log` | Actual row loses its entry cost: four of nine independent cases fail. |
| `family-avenger.log` | Actual row counts Avenger instead of Asgard: five of nine independent cases fail. |
| `restored-build.log` | Nonincremental Release build of Content test project, zero warnings/errors. |
| `restored-focused.log` | 116 passing cases: actual and independent card cases, minion-cost/census prerequisites and reviews, ability-data/lowering and citation gates. |
| `commit.log` | Normal pre-commit architecture, complexity and file-length hooks. |

Each mutant modifies only the actual JSON row, runs the compiled independent
tests, and restores original book bytes in `finally`. Final mutants use the
final fixtures. A nonincremental build and combined restored run follow all
mutants. The combined filter is
`ThorDefender|ThorFamilyFeud|MinionEntryArrowCost|InPlayCardCensus|AbilityData|AbilityLowering|RuleCitationTests`
over fully qualified names in `Marvel.Content.Tests`, Release configuration.
No unnecessary whole-suite run was added to this bounded review.

Scoped verdict: the two actual rows satisfy the reviewed printed behavior and
current Defender rulings. No engine or authored-row correctness finding remains
in this scope. Public Thor stays closed; full-pack, native UI, Hammer Throw and
Get Over Here acceptance remain separate work.
