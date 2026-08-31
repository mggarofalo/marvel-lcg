# Rules Reference v1.7 and v1.8 audit

MARVEL-171 compares the C# engine with the change lists printed on page 1 of
Rules Reference v1.7 and v1.8. The current rules dataset is v1.8. Version 1.7
was read from Fantasy Flight Games' published PDF; it is an audit input, not a
new vendored dataset.

The question in this table is narrower than whether the engine implements every
rule in either book: did a listed revision change observable core-set play?

| Revision | Core-set result |
|---|---|
| v1.7 choose an option | Choice prompts filter options that cannot satisfy `rr:choose-option.1` or `.2`. The ability DSL asks for one option at a time; its authored catalog has no multi-select instruction to which `rr:choose-option.3` can apply. |
| v1.7 otherwise | `otherwise` resolves only when the preceding effect resolves nothing, including false conditions, prohibited effects, missing targets, and effects suspended for a choice (`rr:otherwise`). |
| v1.7 changed controllers | An upgrade attached to another player's card enters that player's play area while retaining its owner (`rr:ownership-and-control.2.1`). Overkill snapshots an ally's controller before defeat moves it to its owner's discard pile (`rr:overkill.1`). Card-specific ways to choose another controller remain card authoring under MARVEL-68. |
| v1.7 referential abilities | Shared titles follow the three-tier association order. Rule-cited tests distinguish an identity from a same-titled ally when the referring source is identity-specific, an obligation, a nemesis card, or an identity side-deck card (`rr:referential-ability.step.2`). |
| v1.7 reveal | The engine turns a card faceup, places it by type, resolves **When Revealed**, and discards a treachery as four ordered steps. Responses to every sub-step wait until all four finish (`rr:reveal.8`), and an attachment without **Attach To** remains in the revealing area and out of play (`rr:reveal.1`). Ability-directed attachment bypasses the printed phrase (`rr:attach-to.3.1`). |
| v1.7 unique | The supported unique-title distinction is cited at `rr:unique-icon.1.2`. Deckbuilding, identity selection, play, put-into-play, and encounter-reveal enforcement belong to MARVEL-272, which owns the product boundary that makes matching unique cards constructible. |
| v1.8 keyword, encounter-icon, and status-card language | Core keywords and status replacements use the v1.8 entries and readable rule citations. The encounter icons used by the core setup are structured card facts. |
| v1.8 simultaneous timing priority | `TimingPriority` matches the five v1.8 tiers, including the nested status-card, forced, and optional priorities. Existing timing tests exercise player ordering and re-reading the board between simultaneous abilities. |
| v1.8 enemy attack procedure | Steps 4 and 5 are separate agenda occurrences. Step 4 fixes the damage; step 5 deals that saved amount (`rr:attack-enemy-activation.step.4` and `.step.5`). |
| v1.8 overkill | Excess damage is dealt after the attacked character is defeated, prevention stops the spill, and an ally's current controller—not its owner—receives it (`rr:overkill`). |
| v1.8 resolve | No supported core effect tests whether another ability “resolved.” Core surge reminder text is governed by the revised surge rule instead. Future consumers are recorded in MARVEL-264. |
| v1.8 surge | A card with surge is dealt as a facedown encounter card after the current reveal finishes, then revealed in player order. Existing rule-cited surge tests cover printed and gained surge. |
| v1.8 player deck customization and game environments | Environment cards already enter the villain's play area and remain until an effect removes them (`rr:environment` and `.1`). Deck customization belongs to MARVEL-272's product-boundary work. |
| v1.7/v1.8 FAQ and errata | These are collections rather than one behavior. Printed core facts are checked by MARVEL-256, setup facts by MARVEL-261, and a card ruling is applied with that card's behavior under MARVEL-68. |

MARVEL-264 retains only rules whose product surface is still absent from the
engine: multi-select choices, player side schemes, and the later-card or mode
instructions classified to it in the v1.8 record audit. The vendored card
catalog is evidence that a card exists, not that its product surface is
supported: only authored abilities can execute. MARVEL-264 therefore remains
in **Future Expansions** until one of those surfaces enters the ability DSL or
setup model. Uniqueness, deck construction, and alternative modes are tracked
separately by MARVEL-272.
