# Thor Defender and Family Feud actual rows

Base revision `4e22008e`; branch `feat/thor-next-actual`. Add actual full-book
rows `06003` Defender of the Nine Realms and `06027` Family Feud. The exact
authored inventory grows from 589 to 591 faces, including 15 Thor faces.
Public admission, setup recipes and replay versions remain unchanged.

Joined printed authority is `datasets/cards/cards.json`. Defender is a zero-cost
Hero Action (thwart): discard until a minion and engage that minion before the
arrow; remove three threat from a chosen scheme after it. Current vendored
clarifications `ruling:6e301a347a5392fb` and `ruling:a1f4982cfebcd768` require
successful engagement before either removal or Confused cancellation, and
revalidate Patrol acquired during payment. `rr:cost-arrow-icon.1` requires
pre-arrow responses to finish before the post-arrow effect. The row uses the
independently reviewed standalone arrow cost and ordinary chosen-scheme thwart.

`ThorDefenderCardTests` uses the full canonical book. It distinguishes no match,
unique-entry prevention, successful engagement while Confused, hero/alter-ego
offers, Quickstrike followed by the actual Thor draw before scheme selection,
and Patrol excluding the main scheme while preserving a legal side scheme.
Rejected intermediate choices preserve digest, RNG and outstanding work.
Repeated offer queries capture those baselines before the first query and then
compare cost resolution with an unprobed control. The test fixture explicitly
removes default encounter minions for the no-match observation.

Family Feud has printed starting threat two and places one additional threat
for each Asgard card in play. Its row counts one complete in-play selector;
ordinary encounter reveal owns the starting threat. `ThorFamilyFeudCardTests`
executes that reveal with both Thor and Odinson, foreign-seat Lady Sif and
Asgard, hosted Mjolnir, unhosted Helmet and enemy Loki: six contributing physical
cards and eight final threat. Matching cards in hand, deck, discard, set-aside,
removed, encounter deck/discard, dealt encounters and beneath-card storage in
both orientations contribute nothing. A zero-Asgard control retains exactly
the printed two threat, and later entries do not add more threat.

Evidence is external at `C:/Users/mggar/Source/thor-next-actual-evidence`:

- `restored-build.log`: nonincremental Release build, no warnings or errors.
- `restored-focused.log`: 79 passed, including actual rows, ability-data
  inventory, whole-book lowering and existing minion-cost/census prerequisites.
- `defender-two-threat.log`: changing three removal to two fails the chosen
  side-scheme observations (two failures).
- `defender-no-entry-cost.log`: removing the arrow cost fails engagement,
  Confused, timing and Patrol observations (six failures).
- `family-avenger.log`: changing Asgard to Avenger fails both-form census and
  zero-Asgard controls (three failures).
- `tool-restore.log` and `commit.log`: normal tool restoration and commit hooks.

The data mutants ran against the built actual tests and restored the original
book bytes after each run. The restored build and 79-test run follow all three
mutants. Initial fixture failures are retained in `initial.log` and
`corrected.log`; they are not acceptance evidence. `focused.log` is the earlier
48-test passing run before the additional offer-query control.

This packet does not author Hammer Throw (`06005`) or Get Over Here (`06014`).
Hammer Throw's preserved timing probe must follow the independent `rr:and`
clarification in `thor-signature-b-checkpoint.md`; its full attack, overkill and
payment evidence remains separate. No full-pack, native UI or public readiness
is claimed. Independent content review remains the next integration step.
