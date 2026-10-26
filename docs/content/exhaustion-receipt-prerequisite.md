# Exhaustion effect receipt prerequisite (MARVEL-735)

Base: `dc00284b`. The printed Harvest (`05029`) body in the generated card
catalog requires global Persona-support exhaustion, healing for each support
actually exhausted, and Surge if none was exhausted. This prerequisite supplies
`result: "exhausted"`; no ability-book row or public admission is added.

The receipt counts committed ready-to-exhausted transitions, not selected cards
or currently exhausted cards. A later exhaustion replaces it even when zero.
It survives nested choices and later readying, and excludes cost exhaustion.
`HarvestEffectReceiptTests` injects the printed body and distinguishes resolver
seats, other players' Persona supports, non-Persona supports, allies, out-of-play
cards, existing exhaustion, maximum hit points, and full encounter Surge cleanup.

Projected readiness is not an exhaustion-result receipt. The engine deliberately
refuses projected trees containing exhaustion and this result read, including
prior sequence effects, because neither the area nor labelled-power interpreter
produces the committed receipt. This conservative guard covers inactive branches
and reads preceding exhaustion too; exact projected receipt transfer remains
unsupported. Ordinary live Harvest When Revealed resolution remains supported.
`ExhaustionResultProjectionTests` pairs unsupported reads with fixed-number
controls, proves rejection before resource payment, captures digest, RNG and
pending work before the first query, and compares later execution with an
unprobed control.

Validation evidence is outside the repository in `%TEMP%/harvest-*.log`.
The initial receipt checks passed 22 focused cases and killed separately compiled
count-already-exhausted and retain-positive-receipt mutants. The projection packet
adds a compiled disabled-guard mutant followed by a forced rebuild and restored
focused verification. Normal commit hooks supply architecture, complexity and
source-length checks. Independent prerequisite review and actual row authoring
remain separate work; these tests do not claim a publicly admitted Harvest game.
