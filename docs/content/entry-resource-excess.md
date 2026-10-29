# Source-bound excess entry resources

This engine prerequisite adds `entryResourceExcess: "this"` as a numeric
expression. The spelling is an engine language choice. It reads the completed
entry payment's generated resources minus its paid resources (`rr:cost.4.1`),
not the number of discarded cards, their printed icons, a nested response cost,
or a discount. It composes with the existing `min` expression for printed caps.

The source must still be the bound physical incarnation and match the entry
receipt's card, payer and either pending origin or resulting in-play copy.
A free/unpaid entry has no excess receipt and reads zero. A different source,
payer, stale ability binding or reentered copy reads zero. This uses the
reviewed MARVEL-886 receipt rather than adding gameplay state or serialization.

Synthetic Valkyrie entry-response tests isolate the expression with real payment
and normal sequence resolution; no Valkyrie interpretation is changed in the
production book. Three resource sets yield one, three and five excess resources,
with the last capped at four. They use distinct Core resource and Thor cards.
Free play and unpaid put are zero-effect controls. A direct receipt-query probe
checks pending/entered identity, other source/payer and reentry plus digest,
RNG and pending-work preservation. Malformed fixed arguments are rejected.
These prepared boards are not public starter/deck or complete-game evidence.

Based on the entry operation independently accepted at `77f4559a`.

The first focused run compiled and passed 34 of 36 cases. Two controls exposed
an existing counter-admission gap: placing zero counters still offered an
optional response. Counter target admission now requires a positive possible
amount, while a separate effective sentence keeps a compound ability legal.
Both sequence orders are tested. A restored expanded run then passed 537 of
538 cases; its one regression was the existing encounter-deck discard cost
whose hidden boost receipt is not yet available. The repair extends the existing
future-discard analysis to pending deck-discard costs without reading hidden
cards. Independent zero/positive top-card controls, fixed zero min/product
bounds and the unchanged original cost test pass in a 20-case focused run.

Earlier compiled mutations distinguished generated-versus-paid excess (three
failures, eight controls passing) and zero-counter admission (two failures,
nine controls passing). After the repair, omitting pending discard-cost receipt recognition produced three failures in nine cases; bypassing the fixed-zero bound produced two failures in four cases. Restored sources were rebuilt non-incrementally without warnings; the expanded regression passed all 545 cases. Independent review remains pending. No canonical Hank
row, zero-HP settlement or pack admission is included. The expression uses MARVEL-887's cohesive `Occurrence.Entry` context.
