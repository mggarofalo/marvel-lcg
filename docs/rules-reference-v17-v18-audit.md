# Rules Reference v1.7 and v1.8 audit

MARVEL-171 compares the C# engine with the change lists printed on page 1 of
Rules Reference v1.7 and v1.8. The current rules dataset is v1.8. Version 1.7
was read from Fantasy Flight Games' published PDF; it is an audit input, not a
new vendored dataset.

The question in this table is narrower than whether the engine implements every
rule in either book: did a listed revision change observable core-set play?

| Revision | Core-set result |
|---|---|
| v1.7 choose an option | Choice prompts filter options that cannot satisfy `rr:choose-option.1` or `.2`. |
| v1.7 otherwise | No core-set card prints **otherwise**. Expansion behavior is recorded in MARVEL-264. |
| v1.7 changed controllers | An upgrade attached to another player's card enters that player's play area while retaining its owner (`rr:ownership-and-control.2.1`). Overkill snapshots an ally's controller before defeat moves it to its owner's discard pile (`rr:overkill.1`). Card-specific ways to choose another controller remain card authoring under MARVEL-68. |
| v1.7 referential abilities | Core self-references and identity names are unambiguous in the supported data. The expanded association order for obligations, nemesis sets, and identity side decks has no core collision; it is recorded in MARVEL-264. |
| v1.7 reveal | The engine already turns a card faceup, places it by type, resolves **When Revealed**, and discards a treachery as four ordered steps. No core card has a response that distinguishes the new response-deferral rule; that behavior and attachments without **Attach To** are recorded in MARVEL-264. |
| v1.7 unique | A physical core set cannot construct two matching unique player cards or identities. Runtime and deckbuilding enforcement becomes observable with a larger card pool and is recorded in MARVEL-264. |
| v1.8 keyword, encounter-icon, and status-card language | Core keywords and status replacements use the v1.8 entries and readable rule citations. The encounter icons used by the core setup are structured card facts. |
| v1.8 simultaneous timing priority | `TimingPriority` matches the five v1.8 tiers, including the nested status-card, forced, and optional priorities. Existing timing tests exercise player ordering and re-reading the board between simultaneous abilities. |
| v1.8 enemy attack procedure | Steps 4 and 5 are separate agenda occurrences. Step 4 fixes the damage; step 5 deals that saved amount (`rr:attack-enemy-activation.step.4` and `.step.5`). |
| v1.8 overkill | Excess damage is dealt after the attacked character is defeated, prevention stops the spill, and an ally's current controller—not its owner—receives it (`rr:overkill`). |
| v1.8 resolve | No supported core effect tests whether another ability “resolved.” Core surge reminder text is governed by the revised surge rule instead. Future consumers are recorded in MARVEL-264. |
| v1.8 surge | A card with surge is dealt as a facedown encounter card after the current reveal finishes, then revealed in player order. Existing rule-cited surge tests cover printed and gained surge. |
| v1.8 player deck customization and game environments | These are outside the physical core-set game boundary and are recorded in MARVEL-264. |
| v1.7/v1.8 FAQ and errata | These are collections rather than one behavior. Printed core facts are checked by MARVEL-256, setup facts by MARVEL-261, and a card ruling is applied with that card's behavior under MARVEL-68. |

MARVEL-264 is in the Plane module **Future Expansions**, at low priority. That
keeps valid later-product work visible without making unsupported expansion
rules part of the core-set completion boundary.
