# Thor batch A actual rows — MARVEL-563

Base: `a1932a33`; branch `feat/thor-actual-a`. Eleven authored faces:
`06001b, 06002, 06007, 06008, 06009, 06010, 06013, 06016, 06022, 06023, 06024`.
This is nonpublic authorship pending independent content review, not complete pack acceptance.

Authority: joined `datasets/cards/cards.json`. The queried vendored FAQ has no entries for these faces. Vendored ruling `ruling:8b3af19c1dfc9452` says kit references to Thor refer only to him. Mjolnir therefore affects named Thor, not Odinson; Lady Sif explicitly permits either named form. Neither printed instruction says the named identity must be controlled by the card controller. Tests distinguish another controller's Thor/Odinson from an unrelated identity. This corrects the controller-only hypothesis in the handoff.

The existing ability-book bytes precede the appended rows unchanged. Inventory adds precisely these IDs. Resource rows are explicitly empty and use printed YY/BB/RR. No setup, public admission, engine or wire changes.

## Behavioral evidence

`ThorConstantCardTests`, `ThorResourceCardTests`, `ThorSearchCardTests` and `ThorAttackReprintTests` load the full authored book. Nonpublic fixtures provide both physical identity faces, encounter/player decks and a living villain. Cases cover own deck/discard physical choices, foreign/in-play exclusion, missing-match shuffle, per-round use through form/phase changes, actual Lady Sif payment and optional response, both forms, named Thor exhaustion/departure, controller-only Asgard/Helmet maximums and refill, actual double-resource play, one-energy generation, repeated query state/RNG/pending-work purity, form/exhaustion rejection, aspect payment, basic/labelled attack defeat and ally/surviving controls.

Commands: nonincremental Release build of Marvel.Content.Tests; focused Thor classes plus AbilityData, AbilityProgram and RuleCitation; complete Marvel.Architecture.Tests. Logs are outside the checkout at `C:/Users/mggar/Source/thor-a-restored-build.log`, `thor-a-restored-tests.log`, `thor-a-architecture.log`.

Twelve individually compiled runtime-DSL mutations were exercised in the full book: Worthy round limit and unconditional shuffle; Lady Sif controller binding; Asgard amount; God of Thunder form and exhaust cost; Mjolnir attack and Aerial; Helmet amount; Chase actor and amount; Power aspect. Each run failed a distinguishing behavioral assertion; individual logs are `C:/Users/mggar/Source/thor-a-mutant-<name>.log`. The original bytes were restored, then sources were built nonincrementally before the final green run. The runtime compiler lowers each mutated row when the test process loads its full book.

## Worthy semantic gap and remaining proof

Exact raw engine description fields captured by the actual row tests (the nullable fields render as empty strings in this log):

```
WORTHY OFFER:  |
WORTHY OFFER:  |
WORTHY PROMPT:  |
WORTHY OFFER:  |
WORTHY PROMPT:  |
```

The offer has null `DisplayLabel` and `Description`; the choice has null `DisplayQuestion` and `Description`. These engine fields therefore supply no named deck-and-discard search or unconditional-shuffle explanation. Rendered client fallback text was not measured by this engine-only packet. The action's compiled behavior works, but this missing semantic explanation prevents a complete interaction claim. `AbilitySearchDescription` recognizes deck-only non-title searches; this packet deliberately makes no engine change. The captured probes remain to guide the owning-layer fix.

Dedicated third-Restricted disposal, low-health Helmet departure/defeat, resource-generator suspension/retry, and same-title deck construction across reprints are not new evidence in this packet. These remain specific proof limits for content review. No native, hosted restart, whole-pack admission or replay26 claim follows from the focused run.
