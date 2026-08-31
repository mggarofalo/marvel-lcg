# The core behavioral specification

How the published Core Set authorities become a complete executable contract.
This document defines the derivation. The obligation catalog, legal scene
constructor and transcript runner implement it.

The dependency points in one direction:

```text
published authorities -> obligations -> legal scenes -> transcripts -> engine
```

Implementation code, unit tests and existing feature files may expose a missed
obligation. They never decide what an obligation says or whether it exists.

## Product boundary

The product is the Marvel Champions Core Set represented by
`datasets/setup/setup.json`: five identities, three scenarios in Standard and
Expert modes, and the encounter sets those games can use. An expansion card,
mode, campaign instruction or deckbuilding exception is outside this contract
even when the Rules Reference describes it.

The Rules Reference is nevertheless enumerated in full. Starting with the
whole document and classifying records out is what makes the boundary
reviewable; starting with rules the implementation cites would make an absent
implementation invisible.

## Canonical authorities

Four committed, offline sources contribute behavior:

| Authority | Source unit | Stable source id |
|---|---|---|
| Rules Reference v1.8 | one citable record | `rr:player-deck.1` |
| generated card catalog | one printed face | `card:01149` |
| official rulings | one ruling or one FAQ record | `ruling:...` or `faq:01001a` |
| authored setup catalog | one hero, scenario, encounter set or campaign record | `setup:hero:iron_man` |

The authority order is the published one in `rr:the-golden-rules`: the Rules
Reference prevails over Learn to Play, and card or scenario text prevails when
it directly contradicts either rulebook. A current audited rules modification
in `datasets/rules-graph.json` supplies the effective rule text. A ruling can
clarify an authority or modify it where the graph records that relationship;
the catalog never selects among contradictory rulings by date or file order.
An unresolved conflict fails derivation and requires adjudication.

The inputs are used as follows:

- `datasets/rules-reference/index.json` supplies every citable id, fragment and
  hash. `Marvel.Rules.Index resolve` supplies an effective modification.
- `datasets/cards/cards.json` supplies printed Core Set faces. A feature is
  never authored from an implementation table or a live website.
- `datasets/marvelcdb-faq/` and `datasets/rulings/` are enumerated in full.
  Each record is admitted or classified `outside-core`; card links, section
  names and the small audited rules graph help adjudicate that classification
  but do not form an inclusion filter. Research can discover a candidate; only
  a vendored record is an input.
- `datasets/setup/setup.json` defines which printed components make a supported
  game. Its tests are the authority gate described by `setup-dataset.md`.

## Fingerprints

Every source unit has a SHA-256 fingerprint. The fingerprint says which exact
authority text was adjudicated; it is not a hash of a scenario or an engine
result.

- A Rules Reference unit uses the effective record hash returned by the rules
  graph resolver.
- A ruling with a committed `hash` uses that value.
- A card face, FAQ record or setup record uses SHA-256 over the RFC 8785 JSON
  Canonicalization Scheme representation of that record. JCS object-property
  ordering, number serialization and string escaping are part of this
  contract: strings are not Unicode-normalized, control characters use the JCS
  lower-case escapes, and every other Unicode character is emitted as its
  literal UTF-8 bytes rather than an optional `\u` escape.

These rules are a wire format for the catalog. A generator must not substitute
the bytes of a whole dataset, serializer defaults, locale-sensitive ordering or
filesystem order. An unchanged source unit therefore has the same fingerprint
on Windows and Linux, while any normative field change invalidates precisely
the obligations derived from it.

The first implementation pins these reproduction vectors. The inputs are the
named subtrees in the current committed datasets, not their surrounding files:

| Source unit | SHA-256 of JCS bytes |
|---|---|
| card face `cards[card_id = 01001a]` | `141d34bb4dde86154de845afad8526562ee296ada91bea42cd2c3fb2a24b0993` |
| setup hero `heroes.iron_man` | `68764d8f0751996d173c63635b6a60ed31035a9de22795278320e7ff381e87b7` |
| FAQ record `entries[code = 01001a]` | `50c91638ef5792206844d0232bf5d623242141daebaef1e9ea1f84c4270d883f` |

## From a source unit to obligations

An **obligation** is one observable proposition derived from an authority. It
is smaller than a rule entry or card and larger than an assertion. Each source
unit is read for these dimensions, in printed order:

1. the moment or trigger;
2. the state and participant preconditions;
3. mandatory and optional choices;
4. legal targets and target-count bounds;
5. costs and whether they can be paid;
6. each conditional or chosen branch, including a specified no-effect branch;
7. quantities and per-player scaling;
8. ordering, priority and simultaneity;
9. limits and expiry;
10. the observable result, including prompts, events and terminal outcomes.

A distinguishable value in one of those dimensions earns an obligation when a
valid Core Set game can reach it. Two clauses that necessarily produce one
atomic observation may share an obligation; two branches that a mutation can
separate may not.

This decomposition requires rules judgment. Tooling enumerates the source
units, emits stable skeletons and checks completeness, but it does not pretend
to infer game semantics from prose. The reviewed branch keys make that judgment
explicit and deterministic thereafter.

### Identity and ordering

An obligation id has this form:

```text
behavior:<source-id>:<branch-key>
```

For example:

```text
behavior:rr:player-deck.1:empty-with-discard
behavior:card:01149:when-revealed-each-player-discards-three
```

`branch-key` is a reviewed, lower-case ASCII identifier. It describes the
published distinction rather than a method, class or historical defect. An id
does not change when a scenario is reworded or implementation code moves.

Catalog order is source-kind order from the authority table, then source id by
ordinal comparison, then the obligations in their reviewed printed order.
Generated reports and skeletons use only that order.

### Dispositions

Every derived obligation has exactly one semantic disposition. A source unit
whose obligations have different dispositions is summarized as `mixed`; the
individual obligations remain the reviewable claims:

| Disposition | Meaning |
|---|---|
| `executable` | A legal Core Set scene and transcript can reach and distinguish it. |
| `narrower` | A named, more precise obligation carries the same proposition. |
| `no-independent-behavior` | It is a heading, summary, example or definition with no separate result. |
| `not-representable` | It governs physical, social, hidden-to-engine or interpretive procedure rather than engine state. |
| `outside-core` | It requires a component or mode absent from the supported Core Set product. |
| `superseded` | A named later authority obligation replaces this historical answer. |

Every disposition except `executable` names its reason. `narrower` and
`superseded` name the target obligation. `outside-core` names the missing product surface. A source
unit cannot disappear because no code cites it or no scenario was convenient
to write.

Representability and implementation are independent. Every `executable`
obligation has one implementation status:

| Status | Required transcript result |
|---|---|
| `unverified` | No implementation claim has been admitted yet. This is allowed while deriving the catalog, but the completed executable corpus rejects it. |
| `supported` | The transcript reaches and observes the published result. |
| `unimplemented` | A negative transcript reaches the branch and observes the exact named `RulesNotImplementedException`. |

An `unimplemented` obligation also names its tracked work. It cannot be treated
as complete merely because the catalog mentions an exception; the executable
negative transcript is the proof that the engine fails closed rather than
guessing.

## Composition, not multiplication

Coverage belongs to the obligation whose decision is observed. A scenario
that tests a card drawing three cards needs a stocked legal deck, but that
precondition does not cover player-deck reset. The reset, continued draw,
discard boundary and empty-deck-with-empty-discard branches have their own
player-deck obligations and scenarios.

A scenario may satisfy multiple obligations only when its assertions directly
distinguish each one and the decisions cannot be separated without changing
the published operation. Setup steps and incidental behavior never receive
coverage merely because they occurred during the transcript.

Rule obligations define shared behavior. Card obligations reference those
rules and add only what the printed card selects, changes or makes observable.
This prevents a Cartesian product of every card effect with every shared edge
case while keeping the shared edge case executable.

## Legal scenes

Every executable obligation begins with a supported game dealt from
`datasets/setup/setup.json`. A scene constructor may then arrange that game for
the decision under test, subject to these invariants. The concrete typed
vocabulary and failure contract are documented in
[`core-scenes.md`](core-scenes.md).

- every printed component is accounted for exactly once unless a rule creates
  or removes it;
- ownership and identity-specific set membership never change;
- deck construction, copy limits and uniqueness remain legal;
- a card occupies exactly one legal area;
- encounter composition belongs to the selected scenario and modular sets;
- state that affects gameplay is explicit, and randomness uses the game seed.

The constructor may stack an owned deck, move owned cards between legal areas,
set legal damage, threat, counters and status cards, choose a form, or advance
through recorded decisions. It may not declare a miniature starting deck,
invent extra copies, borrow another identity's signature card, or mutate an
internal field that has no rules-valid route to the resulting state.

A boundary scene with one card left in the player deck accounts for the other
39 cards in hand, play or discard. It does not replace the legal deck with one
card. This distinction lets a scenario be small without making its game false.

## Executable transcripts

Gherkin is the readable serialization of a transcript. Each scenario names its
primary `@behavior:` obligation and its direct `@rr:`, `@card:` and `@ruling:`
authorities.

- `Given` invokes only the legal scene constructor.
- `When` records one player or engine decision.
- `Then` observes a prompt, event, public game result or deterministic state.
- `And` retains the kind of the preceding step.

Decisions and assertions alternate as the game requires. A policy is explicit;
the runner does not answer an unmentioned prompt on the scenario's behalf.
Unknown, ambiguous, unused and order-invalid steps fail with their source
location. `specs/self-test/quarantine.feature` remains a proof that a false
behavioral assertion fails, not an exception admitted to the passing suite.

Existing feature files are candidate prose only. A scenario enters the passing
corpus after it is independently derived from catalog obligations, rebuilt with
a legal scene, executed against the C# engine and mutation-checked. Prior
existence or historical success supplies no trust.

## Completion evidence

An executable obligation is complete only when all of the following hold:

1. its source ids and fingerprints resolve;
2. its branch decomposition has been reviewed against the full authority text
   and applicable rulings;
3. one or more legal transcripts directly distinguish it;
4. a `supported` transcript observes the published result, or an
   `unimplemented` transcript observes its exact named
   `RulesNotImplementedException`;
5. those transcripts pass against the C# engine on both supported operating
   systems; and
6. a mutation that removes or reverses the decision is killed, or an equivalent
   mutant is documented beside the obligation with the equivalence argument.

Unit `[Rule]` citations are then audited in reverse: every cited behavior must
map to an authority-derived obligation. A citation can reveal missing catalog
work, but cannot create a behavior the authorities do not state.

## Deterministic checks

The completed toolchain has one offline `--check` path that fails for any of
these conditions:

- a canonical source is absent from the catalog;
- a source fingerprint changed;
- an obligation has no disposition or invalid target;
- an executable obligation has no bound passing transcript for its
  implementation status;
- an `unimplemented` transcript does not reach and observe its exact named
  `RulesNotImplementedException`;
- a transcript names a missing or stale obligation;
- a scene violates a legal-game invariant;
- a code citation cannot be mapped back to the catalog;
- generated catalog order or skeleton output differs; or
- required mutation evidence is absent.

The check writes nothing. Regeneration is a deliberate command that produces a
reviewable diff. The test suite continues to leave `git status` clean.

## Authority changes

When a rule, card, ruling or setup record moves:

1. refresh or edit that dataset under its own provenance contract;
2. regenerate the obligation catalog;
3. review every obligation whose source fingerprint changed;
4. add, remove or revise obligations from the new authority;
5. regenerate transcript skeletons and re-author affected scenarios; and
6. run execution and mutation evidence again.

The fingerprint computes the blast radius. It never blesses an old expected
result against new authority text.
