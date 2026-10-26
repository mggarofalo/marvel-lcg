# Doctor Strange top discard — MARVEL-805

Parent MARVEL-565; base `c1d692a3`. This bounded engine packet adds
`query: "invocationTop"` and test-local actual 09001b/09002 abilities.
Production ability JSON, setup, product admission and UI remain outside scope.

## Authority

Generated `datasets/cards/cards.json` prints Natural Talent as discarding **the
Invocation deck** top once per phase, and Wong as exhausting to choose healing
1 damage from **your identity** or discarding **the Invocation deck** top.
The complete pinned `pack:mc08:the-invocation-deck` binds the separate five-card
deck to Doctor Strange, keeps its top faceup during play, and immediately
recycles its own discard without penalty. The vendored MarvelCDB FAQ and ruling
corpus contain no Wong or Natural Talent ruling changing these instructions.

RR 1.8 `rr:ownership-and-control.3` preserves control unless an ability explicitly
changes it; `rr:ownership-and-control.7.2` routes a departing card to its owner's
equivalent out-of-play area. Actual 01071 Make the Call puts an ally from any
player's discard under the resolving player's control. `rr:you-your` and Wong's
explicit identity wording make that controller's identity the heal recipient.
Neither ownership nor control of Wong changes which typed Invocation deck the
singular printed reference identifies. The implementation handoff's earlier
foreign-seat exclusion is a routing hypothesis, not governing authority.

The query resolves the unique initialized typed deck independently of the acting
seat, exposes only its public top, and authorizes that explicitly named
out-of-play target for generic discard (`rr:in-play-and-out-of-play.4`). Synthetic
boards with two typed decks are outside legal unique-identity setup; raising
rather than choosing the first seat is an engine ambiguity policy. Existing
generic discard, choice, exhaust payment and phase limits compose the actions.

## Evidence

`InvocationTopDiscardTests` has 12 passing cases covering second-seat Strange;
Wong under either controller; full-health discard with no ineffective heal
option; controller-specific healing; last-card recycle/reveal without a
player-deck penalty; Natural Talent form changes and phase reset; rejected
choice/retained prompt, accepted exhaustion and retry; hidden/missing tops;
ambiguous typed decks; and digest/RNG/pending-work query purity followed by
completion against an unprobed control.

The actual Make the Call regression uses its production row, pays Wong's printed
cost, completes its real agenda continuation, then uses the test-local actual
Wong action. Wong remains player 1's property under player 0's control and
discards the top of player 1's Invocation deck. Separate healing cases prove
player 0's identity receives his heal in that control state. This is nonpublic
engine evidence, not complete product or UI acceptance.

## Verification

- New cases: 12 passed.
- Compiled wrong-seat mutant (`Seats.Take(1)`): 9 failed, 3 passed.
- Compiled ambiguity mutant (`decks.Length > 2`): explicit ambiguity rejection
  failed; 11 passed.
- Restored Content test project built Release with `--no-incremental`:
  zero warnings/errors.
- Restored related InvocationTopDiscard, SpellMastery, PhaseAbilityLimit and
  CoreLastingEffectCards tests: 48 passed.
- An initial empty-staging commit attempt passed 66 architecture tests but the
  complexity hook rejected added branches in existing selector dispatch methods.
  Cohesive singular/public-query helper extraction addresses those thresholds;
  closeout reruns the same focused suite and normal hooks.

Logs: `C:/Users/mggar/Source/strange-top-{extended,mutant-seat,mutant-ambiguity,restored-build,restored-tests,commit}.log`.
The lead retains independent review and combined integration gates. No complete
pack, cross-platform, save/replay or native-client acceptance is claimed.

## Independent review

Reviewed source `bc5e1b2f34d333b0be826c84d6ca6dc704008915` against
`c1d692a3` in isolated branch `review/strange-top-discard`. Verdict: accept
this bounded engine packet; no production correction identified. Review checked
the generated 09001b/09002/01071 text, complete mc08 insert, ownership/control
and you/your clauses, and `rr:choose-option.2`. The singular deck reference
survives Wong's legal control transfer; his healing follows the controller.
The public selection delegates to the established typed deck lifecycle instead
of scanning card faces or generic additional areas.

Two added adversarial tests distinguish an untyped additional pile containing
a faceup actual Invocation: it cannot provide the discard branch without a
typed deck, or divert selection from the unique typed deck in another seat.
The no-deck negative control also proves Wong's damaged-identity heal remains
available. No production files changed during review.

Independent Release checks on the review branch:

- Related Content filter (`InvocationTopDiscardTests`, `SpellMastery`,
  `PhaseAbilityLimitTests`, `CoreLastingEffectCardsTests`): 50 passed,
  including all 14 top-discard cases.
- Rules filter `InvocationDeck`: 17 passed.
- View filter `InvocationPrivacyReviewTests`: 1 passed, covering pregame
  concealment, reveal, discard, recycle and hidden shuffle order for both seats.
- `git diff --check`: passed.

Logs live outside the repository at
`C:/Users/mggar/Source/strange-top-discard-review-{extended,deck,privacy}.log`;
the Content TRX is under `strange-top-discard-review-extended-results/`.
These are focused managed engine and projection checks. Complete Strange pack,
public admission, native interaction and cross-platform acceptance remain
outside this review.
