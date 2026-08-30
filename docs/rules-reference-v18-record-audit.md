# Rules Reference v1.8 record audit

This audit classifies all 1,218 citable records in the vendored Rules Reference v1.8. It answers which records need tests and which records should not become coverage targets.

The audit is a work list, not a generated coverage gate. The source of truth for rule text remains `datasets/rules-reference/index.json`. The source of truth for live citations remains `dotnet run --project tools/Marvel.Rules.Index -- citations`.

## Results

| Disposition | Records |
|---|---:|
| Executable and cited | 826 |
| Covered by a narrower rule | 50 |
| Redirect or summary | 122 |
| Not enforceable by an engine | 46 |
| Outside the supported product boundary | 136 |
| Unimplemented behavior | 38 |
| Total | 1,218 |

The six dispositions are mutually exclusive:

- Executable and cited means a behavior test directly cites supported behavior for the record.
- Covered by a narrower rule means a cited child clause states the tested decision more precisely.
- Redirect or summary means the record points to, or lists, rules stated elsewhere.
- Not enforceable by an engine means the record defines vocabulary, components, examples, or table procedure.
- Outside the supported product boundary means a named Plane item holds the later product work.
- Unimplemented behavior means a named Plane item owns the missing behavior or citation check.

MARVEL-268 through MARVEL-293 hold the active coverage batches and the
implementation work those batches exposed in the Rules Reference Coverage
module. MARVEL-254 records the one known ordering gap outside those broad work
items.

## Record classifications

| Record | Disposition | Follow-up | Reason |
|---|---|---|---|
| `rr:the-golden-rules` | Not enforceable by an engine | — | This record defines vocabulary, printed anatomy, components, or table procedure rather than a game-state decision. |
| `rr:the-grim-rule` | Not enforceable by an engine | — | This record defines vocabulary, printed anatomy, components, or table procedure rather than a game-state decision. |
| `rr:component-limitations` | Not enforceable by an engine | — | This record defines vocabulary, printed anatomy, components, or table procedure rather than a game-state decision. |
| `rr:round-overview` | Redirect or summary | — | This overview lists rules that the dedicated glossary entries state precisely. |
| `rr:round-overview.step.1` | Redirect or summary | — | This overview lists rules that the dedicated glossary entries state precisely. |
| `rr:round-overview.step.2` | Redirect or summary | — | This overview lists rules that the dedicated glossary entries state precisely. |
| `rr:round-overview.step.3` | Redirect or summary | — | This overview lists rules that the dedicated glossary entries state precisely. |
| `rr:round-overview.step.4` | Redirect or summary | — | This overview lists rules that the dedicated glossary entries state precisely. |
| `rr:round-overview.step.5` | Redirect or summary | — | This overview lists rules that the dedicated glossary entries state precisely. |
| `rr:round-overview.step.6` | Redirect or summary | — | This overview lists rules that the dedicated glossary entries state precisely. |
| `rr:round-overview.step.7` | Redirect or summary | — | This overview lists rules that the dedicated glossary entries state precisely. |
| `rr:round-overview.step.8` | Redirect or summary | — | This overview lists rules that the dedicated glossary entries state precisely. |
| `rr:round-overview.step.9` | Redirect or summary | — | This overview lists rules that the dedicated glossary entries state precisely. |
| `rr:round-overview.step.10` | Redirect or summary | — | This overview lists rules that the dedicated glossary entries state precisely. |
| `rr:ability` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:ability.step.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:ability.step.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:ability.step.2.a` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:ability.step.2.b` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:ability.step.2.c` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:ability.step.3` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:ability.step.4` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:ability.step.4.a` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:ability.step.4.b` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:ability.step.5` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:ability.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:ability.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:ability.3` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:ability.3.1` | Not enforceable by an engine | — | This record is an example, not a separate game-state decision. |
| `rr:ability.4` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:ability.5` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:ability.6` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:ability.7` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:ability.7.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:ability.8` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:ability.8.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:ability.8.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:ability.9` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:ability.10` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:ability.11` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:ability.12` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:ability.13` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:ability.14` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:acceleration-icon` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:acceleration-icon.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:acceleration-icon.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:acceleration-icon.3` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:acceleration-token` | Covered by a narrower rule | — | A directly cited child clause states the executable decision more precisely. |
| `rr:acceleration-token.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:acceleration-token.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:acceleration-token.2.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:acceleration-token.3` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:acceleration-token.4` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:action` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:action.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:action.1.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:action.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:action.2.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:action.2.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:activation` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:activation.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:activation.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:activation.3` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:activation.4` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:activation.5` | Unimplemented behavior | MARVEL-275 | Simultaneous enemy activations currently use fixed order instead of the player's chosen order. |
| `rr:activation.6` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:activation.7` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:activation.8` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:activation.8.1` | Unimplemented behavior | MARVEL-275 | Multiple nested activations need a first-player ordering affordance. |
| `rr:activation.8.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:active-player` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:additional` | Redirect or summary | — | This record redirects readers to Alteration Effect; the destination carries the rule. |
| `rr:after` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:all-purpose-counter` | Not enforceable by an engine | — | This record defines vocabulary, printed anatomy, components, or table procedure rather than a game-state decision. |
| `rr:all-purpose-counter.1` | Executable and cited | — | Typed counters share the card's token inventory, and a behavior test directly cites this record. |
| `rr:all-purpose-counter.2` | Executable and cited | — | Generic counter references count every typed pool and spend an unambiguous pool; behavior tests directly cite this record. |
| `rr:all-purpose-counter.3` | Executable and cited | — | `AllPurposeCounterTests` holds moved counters to the destination card's counter type. |
| `rr:alliance` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:alliance.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:alliance.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:ally` | Covered by a narrower rule | — | A directly cited child clause states the executable decision more precisely. |
| `rr:ally.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:ally.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:ally.3` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:ally.4` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:ally.5` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:ally-limit` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:already` | Redirect or summary | — | This record redirects readers to Alteration Effect; the destination carries the rule. |
| `rr:alteration-effect` | Unimplemented behavior | MARVEL-293 | The iterative and suspended-resolution work item owns this executable rule. |
| `rr:alteration-effect.1` | Unimplemented behavior | MARVEL-293 | The iterative and suspended-resolution work item owns this executable rule. |
| `rr:alter-ego-alter-ego-form` | Redirect or summary | — | This record redirects readers to Form, Identity; the destination carries the rule. |
| `rr:amplify-icon` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:amplify-icon.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:and` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:and.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:and.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:arrow-icon` | Redirect or summary | — | This record redirects readers to Cost Arrow Icon; the destination carries the rule. |
| `rr:aspect-card` | Outside the supported product boundary | MARVEL-272 | This rule belongs to deckbuilding, a mode, or a product classification not yet supported. |
| `rr:aspect-card.1` | Outside the supported product boundary | MARVEL-272 | This rule belongs to deckbuilding, a mode, or a product classification not yet supported. |
| `rr:aspect-card.2` | Outside the supported product boundary | MARVEL-272 | This rule belongs to deckbuilding, a mode, or a product classification not yet supported. |
| `rr:assault` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:assault.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:assault.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:assault.3` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:atk` | Redirect or summary | — | This record redirects readers to Attack (Player Ability Type), Basic Power; the destination carries the rule. |
| `rr:attachment` | Redirect or summary | — | The dedicated Attach To entry states the executable attachment procedure more precisely. |
| `rr:attachment.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:attachment.1.1` | Outside the supported product boundary | MARVEL-264 | No supported card or mode currently reaches this rule; Future Expansions owns it when that product surface opens. |
| `rr:attachment.1.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:attachment.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:attachment.2.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:attach-to` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:attach-to.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:attach-to.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:attach-to.3` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:attach-to.3.1` | Outside the supported product boundary | MARVEL-264 | No supported card or mode currently reaches this rule; Future Expansions owns it when that product surface opens. |
| `rr:attack-enemy-activation` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:attack-enemy-activation.step.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:attack-enemy-activation.step.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:attack-enemy-activation.step.3` | Covered by a narrower rule | — | A directly cited child clause states the executable decision more precisely. |
| `rr:attack-enemy-activation.step.3.a` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:attack-enemy-activation.step.3.b` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:attack-enemy-activation.step.3.c` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:attack-enemy-activation.step.3.d` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:attack-enemy-activation.step.3.e` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:attack-enemy-activation.step.4` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:attack-enemy-activation.step.5` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:attack-enemy-activation.step.6` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:attack-enemy-activation.step.6.a` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:attack-enemy-activation.step.6.b` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:attack-enemy-activation.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:attack-enemy-activation.1.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:attack-enemy-activation.1.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:attack-enemy-activation.1.3` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:attack-enemy-activation.1.4` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:attack-enemy-activation.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:attack-enemy-activation.2.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:attack-enemy-activation.2.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:attack-enemy-activation.3` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:attack-enemy-activation.3.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:attack-enemy-activation.3.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:attack-enemy-activation.4` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:attack-enemy-activation.4.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:attack-enemy-activation.5` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:attack-enemy-activation.6` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:attack-enemy-activation.7` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:attack-enemy-activation.7.1` | Not enforceable by an engine | — | This record is an example, not a separate game-state decision. |
| `rr:attack-player-ability-type` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:attack-player-ability-type.step.7` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:attack-player-ability-type.step.8` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:attack-player-ability-type.step.9` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:attack-player-ability-type.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:attack-player-ability-type.1.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:attack-player-ability-type.1.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:attack-player-ability-type.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:attack-player-ability-type.2.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:attack-player-ability-type.2.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:attack-player-ability-type.3` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:attack-player-ability-type.3.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:attack-player-ability-type.4` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:attack-player-ability-type.5` | Covered by a narrower rule | — | A directly cited child clause states the executable decision more precisely. |
| `rr:attack-player-ability-type.5.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:attack-player-ability-type.6` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:attacks-against-allies` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:attacks-against-allies.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:attacks-against-allies.1.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:attacks-against-allies.1.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:attacks-against-allies.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:attacks-against-allies.3` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:base-value` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:basic-card` | Outside the supported product boundary | MARVEL-272 | This rule belongs to deckbuilding, a mode, or a product classification not yet supported. |
| `rr:basic-card.1` | Outside the supported product boundary | MARVEL-272 | This rule belongs to deckbuilding, a mode, or a product classification not yet supported. |
| `rr:basic-card.2` | Outside the supported product boundary | MARVEL-272 | This rule belongs to deckbuilding, a mode, or a product classification not yet supported. |
| `rr:basic-card.3` | Outside the supported product boundary | MARVEL-272 | This rule belongs to deckbuilding, a mode, or a product classification not yet supported. |
| `rr:basic-power` | Redirect or summary | — | This entry summarizes the dedicated attack, thwart, defense, recovery, and scheme entries. |
| `rr:basic-power.1` | Redirect or summary | — | This record lists the five powers stated by the dedicated entries. |
| `rr:basic-power.1.1` | Redirect or summary | — | The attack ability-type entry states this power more precisely. |
| `rr:basic-power.1.2` | Redirect or summary | — | The thwart entry states this power more precisely. |
| `rr:basic-power.1.3` | Redirect or summary | — | The defend entry states this power more precisely. |
| `rr:basic-power.1.4` | Redirect or summary | — | The recover entry states this power more precisely. |
| `rr:basic-power.1.5` | Redirect or summary | — | The enemy-scheme entry states this power more precisely. |
| `rr:boost-boost-icon` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:boost-boost-icon.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:boost-boost-icon.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:boost-boost-icon.3` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:boost-boost-icon.4` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:boost-boost-icon.5` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:boost-boost-icon.6` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:boost-boost-icon.6.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:campaign-mode` | Redirect or summary | — | This record redirects readers to Modes of Play; the destination carries the rule. |
| `rr:campaign-specific-card` | Outside the supported product boundary | MARVEL-272 | This rule belongs to deckbuilding, a mode, or a product classification not yet supported. |
| `rr:campaign-specific-card.1` | Outside the supported product boundary | MARVEL-272 | This rule belongs to deckbuilding, a mode, or a product classification not yet supported. |
| `rr:campaign-specific-card.2` | Outside the supported product boundary | MARVEL-272 | This rule belongs to deckbuilding, a mode, or a product classification not yet supported. |
| `rr:campaign-specific-card.3` | Outside the supported product boundary | MARVEL-272 | This rule belongs to deckbuilding, a mode, or a product classification not yet supported. |
| `rr:cancel` | Covered by a narrower rule | — | A directly cited child clause states the executable decision more precisely. |
| `rr:cancel.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:cancel.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:cancel.3` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:cancel.4` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:cancel.5` | Outside the supported product boundary | MARVEL-264 | No supported card or mode currently reaches this rule; Future Expansions owns it when that product surface opens. |
| `rr:cancel.5.1` | Outside the supported product boundary | MARVEL-264 | No supported card or mode currently reaches this rule; Future Expansions owns it when that product surface opens. |
| `rr:cannot` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:cannot.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:cannot.2` | Executable and cited | — | A behavior test directly proves an ordinary permission cannot override a prohibition. |
| `rr:cannot.3` | Executable and cited | — | A behavior test directly proves an explicitly marked card instruction can override a prohibition. |
| `rr:card-ability` | Redirect or summary | — | This record redirects readers to Ability; the destination carries the rule. |
| `rr:card-types` | Not enforceable by an engine | — | This record defines the term; the following clauses and dedicated entries carry the decisions. |
| `rr:card-types.1` | Redirect or summary | — | This record lists the dedicated player-card type entries. |
| `rr:card-types.2` | Redirect or summary | — | This record lists the dedicated encounter-card type entries. |
| `rr:card-types.3` | Outside the supported product boundary | MARVEL-264 | The expansion-only revision batch owns player side schemes and their encounter-side interactions. |
| `rr:card-types.3.1` | Outside the supported product boundary | MARVEL-264 | The expansion-only revision batch owns player side schemes and their encounter-side interactions. |
| `rr:card-types.3.2` | Outside the supported product boundary | MARVEL-264 | The expansion-only revision batch owns player side schemes and their encounter-side interactions. |
| `rr:card-types.3.3` | Outside the supported product boundary | MARVEL-264 | The expansion-only revision batch owns player side schemes and their encounter-side interactions. |
| `rr:character` | Not enforceable by an engine | — | This record defines vocabulary, printed anatomy, components, or table procedure rather than a game-state decision. |
| `rr:choose-game-element` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:choose-game-element.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:choose-game-element.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:choose-game-element.3` | Covered by a narrower rule | — | A directly cited child clause states the executable decision more precisely. |
| `rr:choose-game-element.3.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:choose-game-element.4` | Outside the supported product boundary | MARVEL-264 | No supported card or mode currently reaches this rule; Future Expansions owns it when that product surface opens. |
| `rr:choose-option` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:choose-option.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:choose-option.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:choose-option.2.1` | Outside the supported product boundary | MARVEL-264 | No supported card or mode currently reaches this rule; Future Expansions owns it when that product surface opens. |
| `rr:choose-option.2.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:choose-option.3` | Outside the supported product boundary | MARVEL-264 | No supported card or mode currently reaches this rule; Future Expansions owns it when that product surface opens. |
| `rr:classifications` | Outside the supported product boundary | MARVEL-272 | This rule belongs to deckbuilding, a mode, or a product classification not yet supported. |
| `rr:classifications.1` | Outside the supported product boundary | MARVEL-272 | This rule belongs to deckbuilding, a mode, or a product classification not yet supported. |
| `rr:classifications.2` | Outside the supported product boundary | MARVEL-272 | This rule belongs to deckbuilding, a mode, or a product classification not yet supported. |
| `rr:classifications.3` | Outside the supported product boundary | MARVEL-272 | This rule belongs to deckbuilding, a mode, or a product classification not yet supported. |
| `rr:classifications.4` | Outside the supported product boundary | MARVEL-272 | This rule belongs to deckbuilding, a mode, or a product classification not yet supported. |
| `rr:classifications.5` | Outside the supported product boundary | MARVEL-272 | This rule belongs to deckbuilding, a mode, or a product classification not yet supported. |
| `rr:classifications.6` | Outside the supported product boundary | MARVEL-272 | This rule belongs to deckbuilding, a mode, or a product classification not yet supported. |
| `rr:classifications.7` | Outside the supported product boundary | MARVEL-272 | This rule belongs to deckbuilding, a mode, or a product classification not yet supported. |
| `rr:classifications.8` | Outside the supported product boundary | MARVEL-272 | This rule belongs to deckbuilding, a mode, or a product classification not yet supported. |
| `rr:confuse-confused` | Covered by a narrower rule | — | A directly cited child clause states the executable decision more precisely. |
| `rr:confuse-confused.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:confuse-confused.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:confuse-confused.3` | Covered by a narrower rule | — | A directly cited child clause states the executable decision more precisely. |
| `rr:confuse-confused.3.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:confuse-confused.4` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:confuse-confused.5` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:confuse-confused.5.1` | Unimplemented behavior | MARVEL-287 | The dynamic hit-point and status implementation work item owns this executable rule. |
| `rr:confuse-confused.6` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:confuse-confused.7` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:consequential-damage` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:consequential-damage.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:consequential-damage.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:consequential-damage.2.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:consequential-damage.2.2` | Not enforceable by an engine | — | This record is an example, not a separate game-state decision. |
| `rr:constant-ability` | Redirect or summary | — | This record redirects readers to Ability; the destination carries the rule. |
| `rr:control` | Redirect or summary | — | This record redirects readers to Ownership and Control; the destination carries the rule. |
| `rr:copy` | Outside the supported product boundary | MARVEL-272 | Title matching becomes executable when deckbuilding and uniqueness checks cross product boundaries. |
| `rr:cost` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:cost.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:cost.1.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:cost.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:cost.2.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:cost.3` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:cost.3.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:cost.4` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:cost.4.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:cost.4.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:cost.5` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:cost.5.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:cost.6` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:cost.7` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:cost.7.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:cost.7.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:cost.8` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:cost.9` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:cost.10` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:cost.11` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:cost.12` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:cost-arrow-icon` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:cost-arrow-icon.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:counter` | Redirect or summary | — | This record redirects readers to All-Purpose Counter; the destination carries the rule. |
| `rr:crisis-icon` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:crisis-icon.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:crisis-icon.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:damage` | Covered by a narrower rule | — | A directly cited child clause states the executable decision more precisely. |
| `rr:damage.step.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:damage.step.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:damage.step.3` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:damage.step.4` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:damage.step.5` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:damage.step.6` | Unimplemented behavior | MARVEL-275 | Forced interrupts run, but optional interrupts cannot yet suspend damage for a player decision. |
| `rr:damage.step.7` | Unimplemented behavior | MARVEL-275 | Forced defeat abilities run, but optional interrupt and response choices cannot yet suspend defeat. |
| `rr:damage.step.8` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:damage.step.9` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:damage.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:damage.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:damage.3` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:damage.3.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:damage.3.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:damage.3.3` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:dash-value` | Covered by a narrower rule | — | A directly cited child clause states the executable decision more precisely. |
| `rr:dash-value.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:dash-value.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:dash-value.3` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:deal-deal-an-encounter-card` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:deal-deal-an-encounter-card.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:deck` | Redirect or summary | — | This record lists the four dedicated deck entries. |
| `rr:deck.1` | Not enforceable by an engine | — | This defines the physical deck component and its facedown presentation. |
| `rr:deck-customization` | Redirect or summary | — | This record redirects readers to Appendix I: Deck Customization; the destination carries the rule. |
| `rr:def` | Redirect or summary | — | This record redirects readers to Basic Power, Defend; the destination carries the rule. |
| `rr:defeat` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:defeat.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:defeat.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:defend-defense` | Covered by a narrower rule | — | A directly cited child clause states the executable decision more precisely. |
| `rr:defend-defense.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:defend-defense.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:defend-defense.2.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:defend-defense.2.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:defend-defense.3` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:defend-defense.3.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:defend-defense.3.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:defend-defense.3.3` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:defend-defense.4` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:defend-defense.4.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:defend-defense.4.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:defend-defense.4.3` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:defend-defense.4.4` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:defend-defense.4.5` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:defend-defense.4.6` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:defend-defense.4.7` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:defend-defense.4.8` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:defend-defense.5` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:defend-defense.5.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:defend-defense.5.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:defend-defense.6` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:defend-defense.7` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:defend-defense.7.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:defend-defense.7.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:delayed-effect` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:delayed-effect.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:delayed-effect.1.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:delayed-effect.2` | Outside the supported product boundary | MARVEL-264 | No supported card currently exposes whether the delayed effect opens a new triggered-ability window. |
| `rr:discard` | Covered by a narrower rule | — | A directly cited child clause states the executable decision more precisely. |
| `rr:discard.1` | Executable and cited | — | `LifecycleRulesTests` holds player cards faceup on their owner's discard pile. |
| `rr:discard.2` | Executable and cited | — | `LifecycleRulesTests` holds encounter cards faceup on the encounter discard pile. |
| `rr:discard.3` | Outside the supported product boundary | MARVEL-275 | Resumable simultaneous-card ordering owns the affected-player choice. |
| `rr:discard.4` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:discard.4.1` | Outside the supported product boundary | MARVEL-275 | Resumable response windows own the defeat-before-discard ordering choice. |
| `rr:discard.4.2` | Outside the supported product boundary | MARVEL-275 | Resumable response windows own the defeat-before-discard ordering choice. |
| `rr:discard.5` | Outside the supported product boundary | MARVEL-264 | The expansion-only revision batch owns player side schemes. |
| `rr:discard-pile` | Covered by a narrower rule | — | A directly cited child clause states the executable decision more precisely. |
| `rr:discard-pile.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:discard-pile.2` | Not enforceable by an engine | — | This states that discard-pile contents are open information to the players. |
| `rr:discard-pile.3` | Not enforceable by an engine | — | This constrains unsolicited physical reordering rather than defining an engine decision. |
| `rr:discard-pile.4` | Executable and cited | — | `LifecycleRulesTests` holds an empty encounter deck's immediate discard reshuffle. |
| `rr:double-sided-card` | Not enforceable by an engine | — | This defines the double-sided-card component. |
| `rr:double-sided-card.1` | Executable and cited | — | `MainSchemeTests` holds a stage completion to revealing the next side. |
| `rr:double-sided-card.2` | Outside the supported product boundary | MARVEL-264 | The expansion-only revision batch owns player side schemes. |
| `rr:draw-drawing-cards` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:draw-drawing-cards.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:draw-drawing-cards.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:each-player` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:each-player.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:each-player.2` | Outside the supported product boundary | MARVEL-264 | No supported card or mode currently reaches this rule; Future Expansions owns it when that product surface opens. |
| `rr:each-time` | Redirect or summary | — | This record redirects readers to Alteration Effect; the destination carries the rule. |
| `rr:effect` | Redirect or summary | — | This record redirects readers to Ability, Cost; the destination carries the rule. |
| `rr:empty-deck` | Redirect or summary | — | This record redirects readers to Encounter Deck, Player Deck; the destination carries the rule. |
| `rr:encounter-card` | Redirect or summary | — | This record lists the eight dedicated encounter-card type entries. |
| `rr:encounter-card.1` | Redirect or summary | — | The dedicated classification entries state the actual classification rules. |
| `rr:encounter-card.2` | Not enforceable by an engine | — | This record defines vocabulary, printed anatomy, components, or table procedure rather than a game-state decision. |
| `rr:encounter-deck` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:encounter-deck.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:encounter-deck.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:encounter-deck.3` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:encounter-deck.4` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:encounter-discard-pile` | Redirect or summary | — | This record redirects readers to Discard Pile; the destination carries the rule. |
| `rr:encounter-set` | Not enforceable by an engine | — | This record defines an encounter set as a grouping of encounter cards. |
| `rr:encounter-set.1` | Redirect or summary | — | The four dedicated encounter-set entries state the rules for each listed type. |
| `rr:encounter-set.2` | Outside the supported product boundary | MARVEL-272 | This rule belongs to deckbuilding, a mode, or a product classification not yet supported. |
| `rr:end-of-player-phase` | Covered by a narrower rule | — | A directly cited child clause states the executable decision more precisely. |
| `rr:end-of-player-phase.step.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:end-of-player-phase.step.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:end-of-player-phase.step.3` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:end-of-player-phase.step.4` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:end-of-player-phase.step.5` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:enemy` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:enemy-activation` | Redirect or summary | — | This record redirects readers to Activation, Attack (Enemy Activation), Scheme (Enemy; the destination carries the rule. |
| `rr:enemy-attacks` | Redirect or summary | — | This record redirects readers to Activation, Attack (Enemy Activation); the destination carries the rule. |
| `rr:enemy-schemes` | Redirect or summary | — | This record redirects readers to Activation, Scheme (Enemy Activation); the destination carries the rule. |
| `rr:energy-resource` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:energy-resource.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:energy-resource.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:engage` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:engage.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:engage.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:engage.3` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:enters-play` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:environment` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:environment.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:event` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:event.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:event.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:event.3` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:event.4` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:event.5` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:event.5.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:excess-damage` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:exhausted` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:exhausted.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:exhausted.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:expert-mode` | Redirect or summary | — | This record redirects readers to Modes of Play; the destination carries the rule. |
| `rr:expert-set` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:expert-set.1` | Outside the supported product boundary | MARVEL-272 | Selecting or rejecting fixed encounter sets as modular choices needs product-level set validation. |
| `rr:expert-set.2` | Outside the supported product boundary | MARVEL-272 | This rule belongs to deckbuilding, a mode, or a product classification not yet supported. |
| `rr:find` | Outside the supported product boundary | MARVEL-264 | The expansion-only revision batch owns the find instruction. |
| `rr:find.1` | Not enforceable by an engine | — | This defines the term find. |
| `rr:find.2` | Outside the supported product boundary | MARVEL-264 | The expansion-only revision batch owns the find instruction. |
| `rr:find.2.1` | Outside the supported product boundary | MARVEL-264 | The expansion-only revision batch owns the find instruction. |
| `rr:find.2.2` | Outside the supported product boundary | MARVEL-264 | The expansion-only revision batch owns the find instruction. |
| `rr:find.2.3` | Outside the supported product boundary | MARVEL-264 | The expansion-only revision batch owns the find instruction. |
| `rr:find.3` | Outside the supported product boundary | MARVEL-264 | The expansion-only revision batch owns the find instruction. |
| `rr:find.3.1` | Outside the supported product boundary | MARVEL-264 | The expansion-only revision batch owns the find instruction. |
| `rr:find.3.2` | Outside the supported product boundary | MARVEL-264 | The expansion-only revision batch owns the find instruction. |
| `rr:find.3.3` | Outside the supported product boundary | MARVEL-264 | The expansion-only revision batch owns the find instruction. |
| `rr:first-player` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:first-player.1` | Unimplemented behavior | MARVEL-275 | A tied card instruction still stops because the first-player decision cannot suspend and resume it. |
| `rr:first-player.2` | Unimplemented behavior | MARVEL-275 | The resumable-choice work item owns instructions that leave the acting player unspecified. |
| `rr:first-player.3` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:first-player.4` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:first-player.5` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:flip` | Covered by a narrower rule | — | A directly cited child clause states the executable decision more precisely. |
| `rr:flip.1` | Outside the supported product boundary | MARVEL-272 | The supported two-sided identities flip correctly; foldable three-sided identities remain outside the product boundary. |
| `rr:flip.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:flip.2.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:flip.2.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:for-each` | Unimplemented behavior | MARVEL-293 | The iterative and suspended-resolution work item owns this executable rule. |
| `rr:for-each.1` | Unimplemented behavior | MARVEL-293 | The iterative and suspended-resolution work item owns this executable rule. |
| `rr:for-each.1.1` | Not enforceable by an engine | — | This record is an example, not a separate game-state decision. |
| `rr:for-each.2` | Unimplemented behavior | MARVEL-293 | The iterative and suspended-resolution work item owns this executable rule. |
| `rr:for-each.3` | Unimplemented behavior | MARVEL-293 | The iterative and suspended-resolution work item owns this executable rule. |
| `rr:for-each.3.1` | Unimplemented behavior | MARVEL-293 | The iterative and suspended-resolution work item owns this executable rule. |
| `rr:for-each.3.2` | Unimplemented behavior | MARVEL-293 | The iterative and suspended-resolution work item owns this executable rule. |
| `rr:for-each.3.3` | Not enforceable by an engine | — | This record is an example, not a separate game-state decision. |
| `rr:for-each.4` | Unimplemented behavior | MARVEL-293 | The iterative and suspended-resolution work item owns this executable rule. |
| `rr:forced` | Covered by a narrower rule | — | A directly cited child clause states the executable decision more precisely. |
| `rr:forced.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:forced.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:forced.3` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:forced.3.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:forced.4` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:forced.5` | Unimplemented behavior | MARVEL-275 | Encounter reveal fixes the order of simultaneous forced abilities instead of asking the first player. |
| `rr:forced.6` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:form-change-form` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:form-change-form.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:form-change-form.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:form-change-form.3` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:form-change-form.4` | Executable and cited | — | A behavior test directly proves an alter-ego reference has no identity target in hero form. |
| `rr:form-change-form.5` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:form-change-form.6` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:form-change-form.6.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:form-change-form.6.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:form-change-form.7` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:friendly` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:gains` | Executable and cited | — | A behavior test distinguishes runtime possession of gained traits and keywords from printed characteristics. |
| `rr:game-element` | Not enforceable by an engine | — | This record defines vocabulary, printed anatomy, components, or table procedure rather than a game-state decision. |
| `rr:game-element.1` | Not enforceable by an engine | — | This record defines vocabulary, printed anatomy, components, or table procedure rather than a game-state decision. |
| `rr:game-element.2` | Not enforceable by an engine | — | This record defines vocabulary, printed anatomy, components, or table procedure rather than a game-state decision. |
| `rr:game-element.3` | Not enforceable by an engine | — | This record defines vocabulary, printed anatomy, components, or table procedure rather than a game-state decision. |
| `rr:game-element.4` | Not enforceable by an engine | — | This record defines vocabulary, printed anatomy, components, or table procedure rather than a game-state decision. |
| `rr:game-element.5` | Not enforceable by an engine | — | This record defines vocabulary, printed anatomy, components, or table procedure rather than a game-state decision. |
| `rr:game-element.6` | Not enforceable by an engine | — | This record defines vocabulary, printed anatomy, components, or table procedure rather than a game-state decision. |
| `rr:game-element.7` | Not enforceable by an engine | — | This record defines vocabulary, printed anatomy, components, or table procedure rather than a game-state decision. |
| `rr:generate` | Redirect or summary | — | This record redirects readers to Resource; the destination carries the rule. |
| `rr:gets` | Redirect or summary | — | This record redirects readers to Hit Points, Modifiers; the destination carries the rule. |
| `rr:guard` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:guard.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:hand-size` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:hand-size.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:hazard-icon` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:hazard-icon.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:heal` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:heal.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:heal.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:hero-hero-form` | Redirect or summary | — | This record redirects readers to Form, Identity; the destination carries the rule. |
| `rr:heroic-mode` | Redirect or summary | — | This record redirects readers to Modes of Play; the destination carries the rule. |
| `rr:hinder-x` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:hinder-x.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:hinder-x.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:hit-points` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:hit-points.1` | Unimplemented behavior | MARVEL-287 | The dynamic hit-point and status implementation work item owns this executable rule. |
| `rr:hit-points.2` | Covered by a narrower rule | — | A directly cited child clause states the executable decision more precisely. |
| `rr:hit-points.2.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:hit-points.2.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:hit-points.2.3` | Unimplemented behavior | MARVEL-287 | The dynamic hit-point and status implementation work item owns this executable rule. |
| `rr:hit-points.3` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:hit-points.3.1` | Unimplemented behavior | MARVEL-287 | The dynamic hit-point and status implementation work item owns this executable rule. |
| `rr:icons` | Redirect or summary | — | This overview lists rules that the dedicated glossary entries state precisely. |
| `rr:icons.1` | Redirect or summary | — | This overview lists rules that the dedicated glossary entries state precisely. |
| `rr:icons.2` | Redirect or summary | — | This overview lists rules that the dedicated glossary entries state precisely. |
| `rr:icons.3` | Redirect or summary | — | This overview lists rules that the dedicated glossary entries state precisely. |
| `rr:icons.4` | Redirect or summary | — | This overview lists rules that the dedicated glossary entries state precisely. |
| `rr:icons.5` | Redirect or summary | — | This overview lists rules that the dedicated glossary entries state precisely. |
| `rr:icons.6` | Redirect or summary | — | This overview lists rules that the dedicated glossary entries state precisely. |
| `rr:icons.7` | Redirect or summary | — | This overview lists rules that the dedicated glossary entries state precisely. |
| `rr:icons.8` | Redirect or summary | — | This overview lists rules that the dedicated glossary entries state precisely. |
| `rr:icons.9` | Redirect or summary | — | This overview lists rules that the dedicated glossary entries state precisely. |
| `rr:icons.10` | Redirect or summary | — | This overview lists rules that the dedicated glossary entries state precisely. |
| `rr:icons.11` | Redirect or summary | — | This overview lists rules that the dedicated glossary entries state precisely. |
| `rr:icons.12` | Redirect or summary | — | This overview lists rules that the dedicated glossary entries state precisely. |
| `rr:icons.13` | Redirect or summary | — | This overview lists rules that the dedicated glossary entries state precisely. |
| `rr:icons.14` | Redirect or summary | — | This overview lists rules that the dedicated glossary entries state precisely. |
| `rr:identity` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:identity.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:identity.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:identity.3` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:identity.4` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:identity-specific-card` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:identity-specific-card.1` | Outside the supported product boundary | MARVEL-272 | This rule belongs to deckbuilding, a mode, or a product classification not yet supported. |
| `rr:identity-specific-card.2` | Outside the supported product boundary | MARVEL-272 | This rule belongs to deckbuilding, a mode, or a product classification not yet supported. |
| `rr:identity-specific-card.3` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:identity-specific-card.3.1` | Outside the supported product boundary | MARVEL-272 | This rule belongs to deckbuilding, a mode, or a product classification not yet supported. |
| `rr:ignore` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:ignore.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:in-play-and-out-of-play` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:in-play-and-out-of-play.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:in-play-and-out-of-play.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:in-play-and-out-of-play.3` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:in-play-and-out-of-play.4` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:in-play-and-out-of-play.5` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:in-play-and-out-of-play.6` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:in-play-and-out-of-play.7` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:in-play-and-out-of-play.8` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:in-play-and-out-of-play.9` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:in-play-and-out-of-play.10` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:in-play-and-out-of-play.11` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:in-play-and-out-of-play.12` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:in-play-and-out-of-play.13` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:in-player-order` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:in-player-order.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:in-player-order.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:incite-x` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:incite-x.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:indirect-damage` | Covered by a narrower rule | — | A directly cited child clause states the executable decision more precisely. |
| `rr:indirect-damage.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:indirect-damage.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:indirect-damage.3` | Covered by a narrower rule | — | A directly cited child clause states the executable decision more precisely. |
| `rr:indirect-damage.3.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:indirect-damage.3.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:indirect-damage.4` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:indirect-damage.4.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:indirect-damage.5` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:indirect-damage.5.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:indirect-damage.6` | Not enforceable by an engine | — | This record is an example, not a separate game-state decision. |
| `rr:infinite-hit-points` | Redirect or summary | — | This record redirects readers to Hit Points; the destination carries the rule. |
| `rr:initiating-abilities` | Covered by a narrower rule | — | A directly cited child clause states the executable decision more precisely. |
| `rr:initiating-abilities.step.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:initiating-abilities.step.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:initiating-abilities.step.3` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:initiating-abilities.step.4` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:initiating-abilities.step.5` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:initiating-abilities.step.6` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:initiating-abilities.step.7` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:initiating-abilities.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:initiating-abilities.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:initiating-abilities.3` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:instead` | Redirect or summary | — | This record redirects readers to Replacement Effect; the destination carries the rule. |
| `rr:interrupt` | Covered by a narrower rule | — | A directly cited child clause states the executable decision more precisely. |
| `rr:interrupt.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:interrupt.1.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:interrupt.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:interrupt.2.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:interrupt.3` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:interrupt.3.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:interrupt.4` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:interrupt.5` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:keywords` | Redirect or summary | — | This overview lists rules that the dedicated glossary entries state precisely. |
| `rr:keywords.1` | Executable and cited | — | Behavior tests prove numbered addition and non-numeric deduplication across printed and gained instances. |
| `rr:keywords.2` | Redirect or summary | — | This overview lists rules that the dedicated glossary entries state precisely. |
| `rr:keywords.3` | Redirect or summary | — | This overview lists rules that the dedicated glossary entries state precisely. |
| `rr:keywords.4` | Redirect or summary | — | This overview lists rules that the dedicated glossary entries state precisely. |
| `rr:keywords.5` | Redirect or summary | — | This overview lists rules that the dedicated glossary entries state precisely. |
| `rr:keywords.6` | Redirect or summary | — | This overview lists rules that the dedicated glossary entries state precisely. |
| `rr:keywords.7` | Redirect or summary | — | This overview lists rules that the dedicated glossary entries state precisely. |
| `rr:keywords.8` | Redirect or summary | — | This overview lists rules that the dedicated glossary entries state precisely. |
| `rr:keywords.9` | Redirect or summary | — | This overview lists rules that the dedicated glossary entries state precisely. |
| `rr:keywords.10` | Redirect or summary | — | This overview lists rules that the dedicated glossary entries state precisely. |
| `rr:keywords.11` | Redirect or summary | — | This overview lists rules that the dedicated glossary entries state precisely. |
| `rr:keywords.12` | Redirect or summary | — | This overview lists rules that the dedicated glossary entries state precisely. |
| `rr:keywords.13` | Redirect or summary | — | This overview lists rules that the dedicated glossary entries state precisely. |
| `rr:keywords.14` | Redirect or summary | — | This overview lists rules that the dedicated glossary entries state precisely. |
| `rr:keywords.15` | Redirect or summary | — | This overview lists rules that the dedicated glossary entries state precisely. |
| `rr:keywords.16` | Redirect or summary | — | This overview lists rules that the dedicated glossary entries state precisely. |
| `rr:keywords.17` | Redirect or summary | — | This overview lists rules that the dedicated glossary entries state precisely. |
| `rr:keywords.18` | Redirect or summary | — | This overview lists rules that the dedicated glossary entries state precisely. |
| `rr:keywords.19` | Redirect or summary | — | This overview lists rules that the dedicated glossary entries state precisely. |
| `rr:keywords.20` | Redirect or summary | — | This overview lists rules that the dedicated glossary entries state precisely. |
| `rr:keywords.21` | Redirect or summary | — | This overview lists rules that the dedicated glossary entries state precisely. |
| `rr:keywords.22` | Redirect or summary | — | This overview lists rules that the dedicated glossary entries state precisely. |
| `rr:keywords.23` | Redirect or summary | — | This overview lists rules that the dedicated glossary entries state precisely. |
| `rr:keywords.24` | Redirect or summary | — | This overview lists rules that the dedicated glossary entries state precisely. |
| `rr:keywords.25` | Redirect or summary | — | This overview lists rules that the dedicated glossary entries state precisely. |
| `rr:keywords.26` | Redirect or summary | — | This overview lists rules that the dedicated glossary entries state precisely. |
| `rr:keywords.27` | Redirect or summary | — | This overview lists rules that the dedicated glossary entries state precisely. |
| `rr:keywords.28` | Redirect or summary | — | This overview lists rules that the dedicated glossary entries state precisely. |
| `rr:keywords.29` | Redirect or summary | — | This overview lists rules that the dedicated glossary entries state precisely. |
| `rr:labeled-ability` | Executable and cited | — | Labeled abilities share one attributed-performer and cancellation primitive, directly covered by behavior tests. |
| `rr:labeled-ability.1` | Executable and cited | — | A behavior test begins attribution after costs and returns the performing card. |
| `rr:labeled-ability.2` | Executable and cited | — | A behavior test directly attributes an attack-labeled event to the identity. |
| `rr:labeled-ability.3` | Executable and cited | — | A behavior test directly attributes a defense-labeled event to the identity. |
| `rr:labeled-ability.3.1` | Executable and cited | — | Defense resolution uses the attributed performer when establishing the defender role. |
| `rr:labeled-ability.4` | Executable and cited | — | A behavior test directly attributes a thwart-labeled event to the identity. |
| `rr:labeled-ability.5` | Executable and cited | — | One envelope carries every label through attribution and cancellation; an uncanceled combined attack/thwart raises before costs rather than becoming two abilities. |
| `rr:labeled-ability.6` | Executable and cited | — | A behavior test proves one matching status cancels the entire multi-labeled effect. |
| `rr:labeled-ability.6.1` | Executable and cited | — | A canceled labeled ability returns no performer, so no labeled act begins. |
| `rr:labeled-ability.6.2` | Executable and cited | — | A behavior test removes both stunned and confused from an attack/thwart ability. |
| `rr:lasting-effects` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:lasting-effects.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:lasting-effects.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:lasting-effects.3` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:lasting-effects.4` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:lasting-effects.5` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:lasting-effects.6` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:leader` | Outside the supported product boundary | MARVEL-272 | This rule belongs to deckbuilding, campaign, or product classification behavior not yet supported. |
| `rr:leaves-play` | Executable and cited | — | `LifecycleRulesTests` holds an in-play card's move into its out-of-play discard area. |
| `rr:leaves-play.1` | Executable and cited | — | `LifecycleRulesTests` holds a returning card to a new incarnation without its old state. |
| `rr:leaves-play.2` | Executable and cited | — | `LifecycleRulesTests` holds the complete non-villain hosted-state cleanup. |
| `rr:leaves-play.2.1` | Executable and cited | — | `LifecycleRulesTests` holds attached and tucked cards to discard with their host. |
| `rr:leaves-play.2.2` | Executable and cited | — | `LifecycleRulesTests` holds boost cards to discard with their host. |
| `rr:leaves-play.2.3` | Executable and cited | — | `LifecycleRulesTests` holds tokens and status cards to return to the supply. |
| `rr:limit` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:limit.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:linked-card-title` | Executable and cited | — | Printed link resolution is covered by directly cited setup behavior tests. |
| `rr:linked-card-title.1` | Executable and cited | — | Linked product cards are routed to the set-aside area in a directly cited behavior test. |
| `rr:linked-card-title.2` | Executable and cited | — | The cited setup test proves linked cards are excluded from their bringing decks. |
| `rr:linked-card-title.3` | Executable and cited | — | The cited setup test creates one product quantity per bringing deck. |
| `rr:linked-card-title.3.1` | Executable and cited | — | The cited setup test deduplicates repeated bringing cards within one deck. |
| `rr:linked-card-title.4` | Executable and cited | — | A directly cited entry test transfers linked-card ownership to its controller. |
| `rr:look-looked-at` | Outside the supported product boundary | MARVEL-272 | The hidden-information product-boundary item owns look instructions. |
| `rr:look-looked-at.1` | Outside the supported product boundary | MARVEL-272 | The hidden-information product-boundary item owns look instructions. |
| `rr:look-looked-at.1.1` | Outside the supported product boundary | MARVEL-272 | The hidden-information product-boundary item owns look instructions. |
| `rr:loses` | Executable and cited | — | Runtime trait, keyword, and field queries suppress a characteristic while its loss effect is active. |
| `rr:loses.1` | Executable and cited | — | A behavior test proves printed facts retain a characteristic that runtime possession suppresses. |
| `rr:loses.2` | Executable and cited | — | A behavior test proves later grants cannot restore a characteristic while its loss remains active. |
| `rr:main-scheme-main-scheme-deck` | Covered by a narrower rule | — | A directly cited child clause states the executable decision more precisely. |
| `rr:main-scheme-main-scheme-deck.step.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:main-scheme-main-scheme-deck.step.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:main-scheme-main-scheme-deck.step.3` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:main-scheme-main-scheme-deck.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:main-scheme-main-scheme-deck.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:main-scheme-main-scheme-deck.2.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:main-scheme-main-scheme-deck.2.2` | Executable and cited | — | Directly cited card-effect tests advance the deck without setting completion and fail before an unsupported transition. |
| `rr:main-scheme-main-scheme-deck.3` | Covered by a narrower rule | — | The directly cited three enumerated steps state the advance procedure precisely. |
| `rr:main-scheme-main-scheme-deck.4` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:main-scheme-main-scheme-deck.5` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:main-scheme-main-scheme-deck.6` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:max-maximum` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:max-maximum.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:max-maximum.1.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:max-maximum.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:max-maximum.3` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:max-maximum.3.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:max-maximum.4` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:max-maximum.5` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:max-maximum.6` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:maximum-hit-points` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:may` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:mental-resource` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:mental-resource.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:mental-resource.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:minion` | Covered by a narrower rule | — | A directly cited child clause states the executable decision more precisely. |
| `rr:minion.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:minion.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:minion.3` | Unimplemented behavior | MARVEL-275 | The engine currently uses play-area order instead of asking the engaged player. |
| `rr:minion.4` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:modes-of-play` | Outside the supported product boundary | MARVEL-272 | Standard and expert setup are supported; the remaining published modes belong to later product boundaries. |
| `rr:modes-of-play.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:modes-of-play.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:modes-of-play.3` | Outside the supported product boundary | MARVEL-272 | This rule belongs to deckbuilding, a mode, or a product classification not yet supported. |
| `rr:modes-of-play.4` | Outside the supported product boundary | MARVEL-272 | This rule belongs to deckbuilding, a mode, or a product classification not yet supported. |
| `rr:modes-of-play.5` | Outside the supported product boundary | MARVEL-272 | This rule belongs to deckbuilding, a mode, or a product classification not yet supported. |
| `rr:modes-of-play.6` | Outside the supported product boundary | MARVEL-272 | This rule belongs to deckbuilding, a mode, or a product classification not yet supported. |
| `rr:modes-of-play.7` | Outside the supported product boundary | MARVEL-272 | This rule belongs to deckbuilding, a mode, or a product classification not yet supported. |
| `rr:modes-of-play.8` | Outside the supported product boundary | MARVEL-272 | This rule belongs to deckbuilding, a mode, or a product classification not yet supported. |
| `rr:modes-of-play.9` | Outside the supported product boundary | MARVEL-272 | This rule belongs to deckbuilding, a mode, or a product classification not yet supported. |
| `rr:modes-of-play.10` | Outside the supported product boundary | MARVEL-272 | This rule belongs to deckbuilding, a mode, or a product classification not yet supported. |
| `rr:modes-of-play.11` | Outside the supported product boundary | MARVEL-272 | This rule belongs to deckbuilding, a mode, or a product classification not yet supported. |
| `rr:modes-of-play.12` | Outside the supported product boundary | MARVEL-272 | This rule belongs to deckbuilding, a mode, or a product classification not yet supported. |
| `rr:modifiers` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:modifiers.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:modifiers.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:modifiers.3` | Outside the supported product boundary | MARVEL-264 | No supported card or mode currently reaches this rule; Future Expansions owns it when that product surface opens. |
| `rr:modifiers.4` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:modifiers.5` | Outside the supported product boundary | MARVEL-264 | No supported card or mode currently reaches this rule; Future Expansions owns it when that product surface opens. |
| `rr:modifiers.6` | Covered by a narrower rule | — | A directly cited child clause states the executable decision more precisely. |
| `rr:modifiers.6.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:modifiers.7` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:modular-encounter-set` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:modular-encounter-set.1` | Outside the supported product boundary | MARVEL-272 | This rule belongs to deckbuilding, a mode, or a product classification not yet supported. |
| `rr:modular-encounter-set.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:modular-encounter-set.3` | Outside the supported product boundary | MARVEL-272 | This rule belongs to deckbuilding, a mode, or a product classification not yet supported. |
| `rr:move` | Covered by a narrower rule | — | A directly cited child clause states the executable decision more precisely. |
| `rr:move.1` | Executable and cited | — | `MoveDamageTests` holds moving to the same entity to no change. |
| `rr:move.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:move.3` | Covered by a narrower rule | — | A directly cited child clause states the executable decision more precisely. |
| `rr:move.3.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:move.3.2` | Outside the supported product boundary | MARVEL-264 | The expansion-only revision batch owns player side schemes. |
| `rr:move.4` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:move.5` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:move.6` | Outside the supported product boundary | MARVEL-264 | The expansion-only revision batch owns the revised move instruction. |
| `rr:move.7` | Outside the supported product boundary | MARVEL-264 | The expansion-only revision batch owns the revised move instruction. |
| `rr:nemesis-encounter-set` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:nemesis-encounter-set.1` | Outside the supported product boundary | MARVEL-272 | This rule belongs to deckbuilding, campaign, or product classification behavior not yet supported. |
| `rr:nemesis-encounter-set.2` | Outside the supported product boundary | MARVEL-272 | This rule belongs to deckbuilding, campaign, or product classification behavior not yet supported. |
| `rr:nemesis-encounter-set.3` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:nemesis-encounter-set.3.1` | Outside the supported product boundary | MARVEL-264 | No supported card or mode currently reaches this rule; Future Expansions owns it when that product surface opens. |
| `rr:nemesis-encounter-set.4` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:non-numerical-variable` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:non-numerical-variable.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:obligation` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:obligation.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:obligation.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:obligation.3` | Outside the supported product boundary | MARVEL-272 | This rule belongs to deckbuilding, campaign, or product classification behavior not yet supported. |
| `rr:obligation.4` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:obligation.5` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:obligation.6` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:obligation.7` | Outside the supported product boundary | MARVEL-264 | No supported card or mode currently reaches this rule; Future Expansions owns it when that product surface opens. |
| `rr:otherwise` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:otherwise.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:otherwise.1.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:otherwise.1.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:otherwise.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:overkill` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:overkill.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:overkill.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:overkill.3` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:overkill.4` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:overpay` | Redirect or summary | — | This record redirects readers to Cost; the destination carries the rule. |
| `rr:ownership-and-control` | Covered by a narrower rule | — | A directly cited child clause states the executable decision more precisely. |
| `rr:ownership-and-control.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:ownership-and-control.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:ownership-and-control.2.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:ownership-and-control.2.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:ownership-and-control.3` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:ownership-and-control.4` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:ownership-and-control.5` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:ownership-and-control.6` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:ownership-and-control.7` | Covered by a narrower rule | — | A directly cited child clause states the executable decision more precisely. |
| `rr:ownership-and-control.7.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:ownership-and-control.7.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:ownership-and-control.7.3` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:ownership-and-control.7.4` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:ownership-and-control.8` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:patrol` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:patrol.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:pay` | Redirect or summary | — | This record redirects readers to Cost; the destination carries the rule. |
| `rr:per-player-icon` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:per-player-icon.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:peril` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:peril.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:permanent` | Covered by a narrower rule | — | A directly cited child clause states the executable decision more precisely. |
| `rr:permanent.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:permanent.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:permanent.3` | Outside the supported product boundary | MARVEL-272 | Deck construction and deck-size validation are product-boundary work. |
| `rr:permanent.4` | Covered by a narrower rule | — | A directly cited child clause states the executable decision more precisely. |
| `rr:permanent.4.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:permanent.5` | Outside the supported product boundary | MARVEL-264 | The expansion-only revision batch owns permanent attachment reattachment. |
| `rr:permanent.6` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:physical-resource` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:physical-resource.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:physical-resource.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:piercing` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:piercing.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:piercing.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:play-put-into-play` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:play-put-into-play.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:play-put-into-play.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:play-put-into-play.3` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:play-put-into-play.4` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:play-put-into-play.5` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:play-area` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:play-area.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:play-area.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:play-area.3` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:play-restrictions-and-permissions` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:play-restrictions-and-permissions.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:play-restrictions-and-permissions.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:player` | Not enforceable by an engine | — | This record defines vocabulary, printed anatomy, components, or table procedure rather than a game-state decision. |
| `rr:player.1` | Redirect or summary | — | Play-area and ownership entries state each listed area more precisely. |
| `rr:player.2` | Redirect or summary | — | Player-phase and player-turn entries state this round procedure more precisely. |
| `rr:player.3` | Redirect or summary | — | Ownership and control entries state starting ownership more precisely. |
| `rr:player.4` | Redirect or summary | — | The first-player entry states this role more precisely. |
| `rr:player-card` | Redirect or summary | — | The seven dedicated player-card type entries state this list more precisely. |
| `rr:player-card.1` | Redirect or summary | — | The dedicated classification entries state the actual classification rules. |
| `rr:player-card.2` | Not enforceable by an engine | — | This record defines vocabulary, printed anatomy, components, or table procedure rather than a game-state decision. |
| `rr:player-deck` | Covered by a narrower rule | — | A directly cited child clause states the executable decision more precisely. |
| `rr:player-deck.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:player-deck.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:player-deck.3` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:player-deck.4` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:player-discard-pile` | Redirect or summary | — | This record redirects readers to Discard Pile; the destination carries the rule. |
| `rr:player-elimination` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:player-elimination.step.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:player-elimination.step.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:player-elimination.step.3` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:player-elimination.step.4` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:player-elimination.step.5` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:player-elimination.1` | Outside the supported product boundary | MARVEL-264 | The expansion-only revision batch owns permanent attachment reattachment during elimination. |
| `rr:player-elimination.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:player-elimination.3` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:player-elimination.4` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:player-elimination.5` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:player-elimination.5.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:player-elimination.6` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:player-phase` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:player-phase.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:player-side-scheme` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:player-side-scheme.1` | Outside the supported product boundary | MARVEL-272 | This rule belongs to deckbuilding, a mode, or a product classification not yet supported. |
| `rr:player-side-scheme.2` | Outside the supported product boundary | MARVEL-272 | This rule belongs to deckbuilding, a mode, or a product classification not yet supported. |
| `rr:player-side-scheme.3` | Outside the supported product boundary | MARVEL-272 | This rule belongs to deckbuilding, a mode, or a product classification not yet supported. |
| `rr:player-side-scheme.4` | Outside the supported product boundary | MARVEL-272 | This rule belongs to deckbuilding, a mode, or a product classification not yet supported. |
| `rr:player-side-scheme.5` | Outside the supported product boundary | MARVEL-272 | This rule belongs to deckbuilding, a mode, or a product classification not yet supported. |
| `rr:player-side-scheme.5.1` | Outside the supported product boundary | MARVEL-272 | This rule belongs to deckbuilding, a mode, or a product classification not yet supported. |
| `rr:player-side-scheme.5.2` | Outside the supported product boundary | MARVEL-272 | This rule belongs to deckbuilding, a mode, or a product classification not yet supported. |
| `rr:player-side-scheme-limit` | Outside the supported product boundary | MARVEL-272 | This rule belongs to deckbuilding, a mode, or a product classification not yet supported. |
| `rr:player-side-scheme-limit.1` | Outside the supported product boundary | MARVEL-272 | This rule belongs to deckbuilding, a mode, or a product classification not yet supported. |
| `rr:player-side-scheme-limit.2` | Outside the supported product boundary | MARVEL-272 | This rule belongs to deckbuilding, a mode, or a product classification not yet supported. |
| `rr:player-side-scheme-limit.3` | Outside the supported product boundary | MARVEL-272 | This rule belongs to deckbuilding, a mode, or a product classification not yet supported. |
| `rr:player-turn` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:player-turn.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:player-turn.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:player-turn.3` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:player-turn.4` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:player-turn.5` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:player-turn.5.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:player-turn.6` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:player-s-play-area` | Covered by a narrower rule | — | A directly cited child clause states the executable decision more precisely. |
| `rr:player-s-play-area.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:player-s-play-area.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:player-s-play-area.3` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:player-s-play-area.4` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:player-s-play-area.5` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:player-s-play-area.6` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:playing-cards` | Redirect or summary | — | This record redirects readers to Initiating Abilities; the destination carries the rule. |
| `rr:prevent` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:prevent.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:prevent.1.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:prevent.1.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:prevent.1.3` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:prevent.1.4` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:prevent.1.5` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:prevent.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:printed` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:printed.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:printed.1.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:qualifiers` | Executable and cited | — | A behavior test proves one qualifier filters every applicable type in a combined selector. |
| `rr:quickstrike` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:quickstrike.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:quickstrike.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:ranged` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:ranged.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:ready` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:ready.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:ready.1.1` | Unimplemented behavior | MARVEL-275 | The resumable-choice work item owns declining an additional readying cost. |
| `rr:rec` | Redirect or summary | — | This record redirects readers to Basic Power, Recover, Recovery; the destination carries the rule. |
| `rr:recover-recovery` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:recover-recovery.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:referential-ability` | Executable and cited | — | Shared-title references use the cited precedence implemented by the narrower steps. |
| `rr:referential-ability.step.1` | Executable and cited | — | A self-reference beats an identity card sharing the source's title. |
| `rr:referential-ability.step.2` | Executable and cited | — | An identity-associated card beats an unrelated player card with the same title. |
| `rr:referential-ability.step.3` | Executable and cited | — | A player-card reference includes player cards and excludes same-titled encounter cards. |
| `rr:remaining-hit-points` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:remaining-hit-points.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:remaining-hit-points.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:reminder-text` | Not enforceable by an engine | — | This record defines vocabulary, printed anatomy, components, or table procedure rather than a game-state decision. |
| `rr:removed-from-the-game` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:removed-from-the-game.1` | Executable and cited | — | `LifecycleRulesTests` holds removed cards out of play. |
| `rr:removed-from-the-game.2` | Executable and cited | — | `LifecycleRulesTests` holds removed cards against later movement. |
| `rr:replacement-effect` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:replacement-effect.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:requirement-resources` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:requirement-resources.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:requirement-resources.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:resolve` | Unimplemented behavior | MARVEL-293 | The iterative and suspended-resolution work item owns the complete resolution procedure. |
| `rr:resolve.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:resolve.2` | Unimplemented behavior | MARVEL-293 | The iterative and suspended-resolution work item owns this executable rule. |
| `rr:resolve.3` | Unimplemented behavior | MARVEL-293 | The iterative and suspended-resolution work item owns this executable rule. |
| `rr:resolve.4` | Unimplemented behavior | MARVEL-293 | The iterative and suspended-resolution work item owns this executable rule. |
| `rr:resolve.5` | Unimplemented behavior | MARVEL-293 | The iterative and suspended-resolution work item owns this executable rule. |
| `rr:resolve.6` | Unimplemented behavior | MARVEL-293 | The iterative and suspended-resolution work item owns this executable rule. |
| `rr:resolve.7` | Unimplemented behavior | MARVEL-293 | The iterative and suspended-resolution work item owns this executable rule. |
| `rr:resolve.8` | Unimplemented behavior | MARVEL-293 | The iterative and suspended-resolution work item owns this executable rule. |
| `rr:resource` | Covered by a narrower rule | — | A directly cited child clause states the executable decision more precisely. |
| `rr:resource.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:resource.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:resource.3` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:resource.4` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:resource.5` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:resource-ability` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:resource-ability.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:resource-ability.1.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:resource-card` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:resource-card.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:resource-card.2` | Executable and cited | — | The shared performer rule treats a controlled resource card as an extension of its identity, directly covered by a behavior test. |
| `rr:resource-type` | Redirect or summary | — | This record redirects readers to Resource; the destination carries the rule. |
| `rr:response` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:response.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:response.1.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:response.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:response.2.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:response.3` | Unimplemented behavior | MARVEL-275 | Responses to multiple conditions from one effect need a player-chosen order. |
| `rr:response.4` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:restricted` | Unimplemented behavior | MARVEL-275 | The deterministic oldest-card workaround does not ask which restricted card the player chooses to discard. |
| `rr:restricted.1` | Unimplemented behavior | MARVEL-275 | The forced response is implemented with a fixed discard instead of the required player choice. |
| `rr:retaliate-x` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:retaliate-x.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:retaliate-x.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:reveal` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:reveal.step.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:reveal.step.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:reveal.step.3` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:reveal.step.4` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:reveal.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:reveal.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:reveal.3` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:reveal.4` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:reveal.4.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:reveal.5` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:reveal.6` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:reveal.7` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:reveal.8` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:rookie-mode` | Redirect or summary | — | This record redirects readers to Modes of Play; the destination carries the rule. |
| `rr:round-structure` | Redirect or summary | — | This record redirects readers to Overview - Round Overview, End of the Player Phase,; the destination carries the rule. |
| `rr:running-out-of-cards` | Redirect or summary | — | This record redirects readers to Encounter Deck, Player Deck; the destination carries the rule. |
| `rr:scenario-specific-card` | Outside the supported product boundary | MARVEL-272 | This rule belongs to deckbuilding, a mode, or a product classification not yet supported. |
| `rr:scenario-specific-card.1` | Outside the supported product boundary | MARVEL-272 | This rule belongs to deckbuilding, a mode, or a product classification not yet supported. |
| `rr:scenario-specific-card.2` | Outside the supported product boundary | MARVEL-272 | This rule belongs to deckbuilding, a mode, or a product classification not yet supported. |
| `rr:scenario-specific-card.3` | Outside the supported product boundary | MARVEL-272 | This rule belongs to deckbuilding, a mode, or a product classification not yet supported. |
| `rr:sch` | Redirect or summary | — | This record redirects readers to Basic Power, Scheme (Enemy Activation); the destination carries the rule. |
| `rr:scheme-card-type` | Not enforceable by an engine | — | This record classifies the three printed scheme card types; its child clause carries the decision. |
| `rr:scheme-card-type.1` | Unimplemented behavior | MARVEL-275 | The resumable-choice work item owns selecting which main or side scheme an ability affects. |
| `rr:scheme-enemy-activation` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:scheme-enemy-activation.step.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:scheme-enemy-activation.step.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:scheme-enemy-activation.step.2.a` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:scheme-enemy-activation.step.2.b` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:scheme-enemy-activation.step.2.c` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:scheme-enemy-activation.step.2.d` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:scheme-enemy-activation.step.2.e` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:scheme-enemy-activation.step.3` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:search` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:search.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:search.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:search.3` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:search.4` | Outside the supported product boundary | MARVEL-272 | The hidden-information product-boundary item owns searching a collection. |
| `rr:search.4.1` | Outside the supported product boundary | MARVEL-272 | The hidden-information product-boundary item owns searching a collection. |
| `rr:self-referential` | Redirect or summary | — | This record redirects readers to Referential Ability; the destination carries the rule. |
| `rr:set-aside-set-aside` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:set-icon` | Not enforceable by an engine | — | This record defines vocabulary, printed anatomy, components, or table procedure rather than a game-state decision. |
| `rr:setup-keyword` | Covered by a narrower rule | — | A directly cited child clause states the executable decision more precisely. |
| `rr:setup-keyword.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:setup-triggered-ability` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:setup-triggered-ability.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:setup-triggered-ability.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:setup-triggered-ability.3` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:shuffle` | Executable and cited | — | `SearchAndDiscardCardsTests` holds shuffling to randomized card order. |
| `rr:shuffle.1` | Executable and cited | — | `SearchAndDiscardCardsTests` holds a searched deck to shuffle after the search. |
| `rr:shuffle.2` | Executable and cited | — | `SearchAndDiscardCardsTests` holds a searched discard pile to shuffle into its deck. |
| `rr:side-scheme` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:side-scheme.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:side-scheme.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:simultaneous-resolution` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:skirmish-mode` | Redirect or summary | — | This record redirects readers to Modes of Play; the destination carries the rule. |
| `rr:special` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:stalwart` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:stalwart.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:stalwart.2` | Unimplemented behavior | MARVEL-287 | The dynamic hit-point and status implementation work item owns this executable rule. |
| `rr:standard-mode` | Redirect or summary | — | This record redirects readers to Modes of Play; the destination carries the rule. |
| `rr:standard-set` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:standard-set.1` | Outside the supported product boundary | MARVEL-272 | Selecting or rejecting fixed encounter sets as modular choices needs product-level set validation. |
| `rr:standard-set.2` | Outside the supported product boundary | MARVEL-272 | This rule belongs to deckbuilding, a mode, or a product classification not yet supported. |
| `rr:star-icon` | Covered by a narrower rule | — | A directly cited child clause states the executable decision more precisely. |
| `rr:star-icon.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:star-icon.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:star-icon.3` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:star-icon.4` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:star-icon.5` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:star-icon.6` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:status-cards` | Covered by a narrower rule | — | A directly cited child clause states the executable decision more precisely. |
| `rr:status-cards.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:status-cards.1.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:status-cards.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:steady` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:steady.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:stun-stunned` | Covered by a narrower rule | — | A directly cited child clause states the executable decision more precisely. |
| `rr:stun-stunned.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:stun-stunned.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:stun-stunned.3` | Covered by a narrower rule | — | A directly cited child clause states the executable decision more precisely. |
| `rr:stun-stunned.3.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:stun-stunned.4` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:stun-stunned.5` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:stun-stunned.5.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:stun-stunned.6` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:stun-stunned.7` | Unimplemented behavior | MARVEL-287 | The dynamic hit-point and status implementation work item owns this executable rule. |
| `rr:subtitle` | Not enforceable by an engine | — | This record defines vocabulary, printed anatomy, components, or table procedure rather than a game-state decision. |
| `rr:support` | Not enforceable by an engine | — | This record names a printed card type; its clauses carry the executable support rules. |
| `rr:support.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:support.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:support.3` | Executable and cited | — | A behavior test proves an in-play support performs its own labeled ability and does not consume the identity's status. |
| `rr:surge` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:surge.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:surge.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:sustained-damage` | Covered by a narrower rule | — | A directly cited child clause states the executable decision more precisely. |
| `rr:sustained-damage.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:sustained-damage.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:swap` | Outside the supported product boundary | MARVEL-264 | The expansion-only revision batch owns the swap instruction. |
| `rr:swap.1` | Outside the supported product boundary | MARVEL-264 | The expansion-only revision batch owns the swap instruction. |
| `rr:swap.1.1` | Not enforceable by an engine | — | This record is an example, not a separate game-state decision. |
| `rr:swap.2` | Outside the supported product boundary | MARVEL-264 | The expansion-only revision batch owns the swap instruction. |
| `rr:swap.3` | Outside the supported product boundary | MARVEL-264 | The expansion-only revision batch owns the swap instruction. |
| `rr:swap.4` | Outside the supported product boundary | MARVEL-264 | The expansion-only revision batch owns the swap instruction. |
| `rr:swap.4.1` | Outside the supported product boundary | MARVEL-264 | The expansion-only revision batch owns the swap instruction. |
| `rr:swap.4.2` | Outside the supported product boundary | MARVEL-264 | The expansion-only revision batch owns the swap instruction. |
| `rr:table-talk` | Not enforceable by an engine | — | This record defines vocabulary, printed anatomy, components, or table procedure rather than a game-state decision. |
| `rr:table-talk.1` | Not enforceable by an engine | — | This record defines vocabulary, printed anatomy, components, or table procedure rather than a game-state decision. |
| `rr:target` | Covered by a narrower rule | — | A directly cited child clause states the executable decision more precisely. |
| `rr:target.1` | Not enforceable by an engine | — | This record is an example, not a separate game-state decision. |
| `rr:target.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:target.2.1` | Executable and cited | — | Basic attack and thwart behavior tests require valid targets. |
| `rr:target.2.2` | Executable and cited | — | An empty choose selector removes the unanswerable action. |
| `rr:target.2.3` | Executable and cited | — | A draw action requires a card currently in the named player's deck. |
| `rr:target.3` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:target.3.1` | Not enforceable by an engine | — | This record is an example, not a separate game-state decision. |
| `rr:target.3.2` | Executable and cited | — | Zero ATK and THW powers can exhaust against otherwise-valid targets. |
| `rr:target.3.3` | Executable and cited | — | A cost that could create an effect target is excluded from target validity. |
| `rr:target.3.4` | Executable and cited | — | One valid effect target keeps a multi-effect ability initiable. |
| `rr:target.3.5` | Executable and cited | — | A cannot-ready prohibition removes the card from offering and execution. |
| `rr:target.3.6` | Executable and cited | — | Tough prevention leaves the damage target valid and discards Tough on resolution. |
| `rr:target.3.7` | Executable and cited | — | A cannot-take-damage target is excluded from a damage-only group effect. |
| `rr:target.3.8` | Executable and cited | — | Guard removes a prohibited villain from attack targets. |
| `rr:target.3.9` | Executable and cited | — | Patrol removes the prohibited main scheme from thwart targets. |
| `rr:target.4` | Executable and cited | — | A group effect initiates when its villain target remains valid. |
| `rr:target.4.1` | Executable and cited | — | The same group effect skips a minion that cannot take its damage. |
| `rr:target.4.2` | Not enforceable by an engine | — | This record is an example, not a separate game-state decision. |
| `rr:target.5` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:target.6` | Executable and cited | — | A search with a searchable area initiates even when no card matches. |
| `rr:target-threat` | Not enforceable by an engine | — | This record defines printed anatomy; main-scheme clauses carry the executable threshold behavior. |
| `rr:team-up` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:team-up.1` | Outside the supported product boundary | MARVEL-272 | The parent citation proves the play restriction; this combined clause also requires deck-building validation. |
| `rr:team-up.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:teamwork` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:teamwork.1` | Redirect or summary | — | This shorter restatement is covered by the cited parent keyword, which retains the word “other.” |
| `rr:teamwork.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:temporary` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:temporary.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:text-box` | Not enforceable by an engine | — | This record defines vocabulary, printed anatomy, components, or table procedure rather than a game-state decision. |
| `rr:text-box.1` | Outside the supported product boundary | MARVEL-264 | No core-set card ability refers to another card's text box; expansion cards that blank or replace text boxes own the executable behavior. |
| `rr:text-box.1.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:that` | Redirect or summary | — | This record redirects readers to Alteration Effect, Target; the destination carries the rule. |
| `rr:then` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:then.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:then.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:this` | Redirect or summary | — | This record redirects readers to Alteration Effect; the destination carries the rule. |
| `rr:threat` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:thw` | Redirect or summary | — | This record redirects readers to Basic Power, Thwart; the destination carries the rule. |
| `rr:thwart` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:thwart.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:thwart.1.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:thwart.1.2` | Outside the supported product boundary | MARVEL-264 | No supported card or mode currently reaches this rule; Future Expansions owns it when that product surface opens. |
| `rr:thwart.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:thwart.2.1` | Outside the supported product boundary | MARVEL-264 | No supported card or mode currently reaches this rule; Future Expansions owns it when that product surface opens. |
| `rr:thwart.2.2` | Outside the supported product boundary | MARVEL-264 | No supported card or mode currently reaches this rule; Future Expansions owns it when that product surface opens. |
| `rr:timing` | Redirect or summary | — | This record redirects readers to Ability, Interrupt, Response, “Would”; the destination carries the rule. |
| `rr:tough` | Covered by a narrower rule | — | A directly cited child clause states the executable decision more precisely. |
| `rr:tough.1` | Covered by a narrower rule | — | The directly cited `rr:tough.2` states the executable replacement effect more precisely. |
| `rr:tough.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:tough.2.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:tough.2.2` | Unimplemented behavior | MARVEL-287 | The dynamic hit-point and status implementation work item owns this executable rule. |
| `rr:tough.3` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:toughness` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:toughness.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:traits` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:traits.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:traits.2` | Outside the supported product boundary | MARVEL-264 | No supported card ability currently distinguishes traits from printed text-box content. |
| `rr:treachery` | Not enforceable by an engine | — | This record names a printed card type; its clauses carry the executable treachery rules. |
| `rr:treachery.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:treachery.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:treachery.2.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:triggered-ability` | Not enforceable by an engine | — | This record defines printed anatomy, components, or table procedure rather than a game-state decision. |
| `rr:triggering-condition` | Covered by a narrower rule | — | A directly cited child clause states the executable decision more precisely. |
| `rr:triggering-condition.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:triggering-condition.1.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:triggering-condition.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:tuck` | Executable and cited | — | `LifecycleRulesTests` holds tucking beneath a host. |
| `rr:tuck.1` | Executable and cited | — | `LifecycleRulesTests` holds tucked cards faceup and out of play. |
| `rr:tuck.2` | Executable and cited | — | `LifecycleRulesTests` holds tucked cards to discard with their host. |
| `rr:undefended` | Redirect or summary | — | This record redirects readers to Attack (Enemy Activation); the destination carries the rule. |
| `rr:unique-icon` | Covered by a narrower rule | — | A directly cited child clause states the executable decision more precisely. |
| `rr:unique-icon.1` | Covered by a narrower rule | — | A directly cited child clause states the executable decision more precisely. |
| `rr:unique-icon.1.1` | Outside the supported product boundary | MARVEL-272 | The uniqueness work item owns title matching without subtitles or alter-ego titles. |
| `rr:unique-icon.1.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:unique-icon.2` | Outside the supported product boundary | MARVEL-272 | The uniqueness work item owns matching-card deckbuilding limits. |
| `rr:unique-icon.2.1` | Outside the supported product boundary | MARVEL-272 | The uniqueness work item owns matching cards added after setup begins. |
| `rr:unique-icon.3` | Outside the supported product boundary | MARVEL-272 | The uniqueness work item owns identity selection. |
| `rr:unique-icon.3.1` | Outside the supported product boundary | MARVEL-272 | The uniqueness work item owns the identity and villain exception. |
| `rr:unique-icon.4` | Outside the supported product boundary | MARVEL-272 | The uniqueness work item owns matching cards entering play. |
| `rr:unique-icon.4.1` | Outside the supported product boundary | MARVEL-272 | The uniqueness work item owns play and put-into-play checks. |
| `rr:unique-icon.4.2` | Outside the supported product boundary | MARVEL-272 | The uniqueness work item owns encounter-card reveal handling. |
| `rr:upgrade` | Not enforceable by an engine | — | This record names a printed card type; its clauses carry the executable upgrade rules. |
| `rr:upgrade.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:upgrade.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:upgrade.3` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:upgrade.3.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:upgrade.4` | Executable and cited | — | A behavior test covers both identity extension and the different-friendly-character host exception. |
| `rr:uses-x-type` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:uses-x-type.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:valid-target` | Redirect or summary | — | This record redirects readers to Target; the destination carries the rule. |
| `rr:victory-display` | Executable and cited | — | `KeywordTests` holds defeated Victory attachments to the victory display. |
| `rr:victory-x` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:victory-x.1` | Executable and cited | — | `KeywordTests` holds defeated Victory cards to the victory display. |
| `rr:victory-x.1.1` | Executable and cited | — | `KeywordTests` holds defeated Victory attachments to the victory display. |
| `rr:victory-x.1.2` | Executable and cited | — | `KeywordTests` holds Victory attachments to score independently of their host. |
| `rr:victory-x.1.3` | Executable and cited | — | `AllPurposeCounterTests` holds a Victory card discarded at zero uses to the victory display. |
| `rr:victory-x.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:victory-x.3` | Executable and cited | — | `KeywordTests` holds a Victory attachment to the display when its host is defeated. |
| `rr:victory-x.4` | Executable and cited | — | `AllPurposeCounterTests` holds the card's Victory value while it is in the victory display. |
| `rr:victory-x.5` | Executable and cited | — | `KeywordTests` holds the shared display's printed Victory values to a point total. |
| `rr:villain-villain-deck` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:villain-villain-deck.1` | Outside the supported product boundary | MARVEL-264 | No supported card or mode currently reaches this rule; Future Expansions owns it when that product surface opens. |
| `rr:villain-defeat` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:villain-defeat.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:villain-defeat.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:villain-defeat.3` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:villain-defeat.3.1` | Outside the supported product boundary | MARVEL-264 | No supported card or mode currently reaches this rule; Future Expansions owns it when that product surface opens. |
| `rr:villain-defeat.3.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:villain-defeat.3.3` | Outside the supported product boundary | MARVEL-264 | No supported card or mode currently reaches this rule; Future Expansions owns it when that product surface opens. |
| `rr:villain-defeat.4` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:villain-defeat.4.1` | Outside the supported product boundary | MARVEL-264 | No supported card or mode currently reaches this rule; Future Expansions owns it when that product surface opens. |
| `rr:villain-defeat.4.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:villain-defeat.4.3` | Outside the supported product boundary | MARVEL-264 | No supported card or mode currently reaches this rule; Future Expansions owns it when that product surface opens. |
| `rr:villain-phase` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:villain-phase.step.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:villain-phase.step.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:villain-phase.step.2.a` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:villain-phase.step.2.b` | Unimplemented behavior | MARVEL-275 | The engine currently uses play-area order instead of asking the engaged player. |
| `rr:villain-phase.step.3` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:villain-phase.step.4` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:villain-phase.step.5` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:villain-phase.step.6` | Covered by a narrower rule | — | A directly cited child clause states the executable decision more precisely. |
| `rr:villain-phase.step.6.a` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:villain-phase.step.6.b` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:villain-s-play-area` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:villain-s-play-area.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:villain-s-play-area.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:villain-s-play-area.3` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:villain-s-play-area.4` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:villainous` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:villainous.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:villainous.2` | Outside the supported product boundary | MARVEL-264 | No supported card or mode currently reaches this rule; Future Expansions owns it when that product surface opens. |
| `rr:vulnerable` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:vulnerable.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:vulnerable.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:vulnerable.3` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:when-completed-abilities` | Covered by a narrower rule | — | A directly cited child clause states the executable decision more precisely. |
| `rr:when-completed-abilities.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:when-defeated-abilities` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:when-defeated-abilities.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:when-defeated-abilities.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:when-defeated-abilities.2.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:when-revealed-abilities` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:when-revealed-abilities.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:when-revealed-abilities.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:when-revealed-abilities.3` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:wild-resource` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:wild-resource.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:wild-resource.1.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:wild-resource.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:wild-resource.3` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:winning-the-game` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:winning-the-game.1` | Outside the supported product boundary | MARVEL-264 | No supported card or mode currently reaches this rule; Future Expansions owns it when that product surface opens. |
| `rr:winning-the-game.1.1` | Outside the supported product boundary | MARVEL-264 | No supported card or mode currently reaches this rule; Future Expansions owns it when that product surface opens. |
| `rr:would` | Covered by a narrower rule | — | A directly cited child clause states the executable decision more precisely. |
| `rr:would.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:x-value` | Redirect or summary | — | This record redirects readers to Non-Numerical Variable; the destination carries the rule. |
| `rr:you-your` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:you-your.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:you-your.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:you-your.3` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:you-your.4` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:you-your.5` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:you-your.6` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:you-your.7` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:you-your.8` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:you-your.9` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:you-your.10` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:you-your.11` | Outside the supported product boundary | MARVEL-264 | No supported card or mode currently reaches this rule; Future Expansions owns it when that product surface opens. |
| `rr:you-your.12` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:you-your.13` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:you-your.14` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:you-your.15` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:you-your.16` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:you-your.17` | Outside the supported product boundary | MARVEL-272 | This rule belongs to deckbuilding, campaign, or product classification behavior not yet supported. |
| `rr:you-your.18` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:appendix-ii-setup` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:appendix-ii-setup.step.1` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:appendix-ii-setup.step.2` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:appendix-ii-setup.step.3` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:appendix-ii-setup.step.4` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:appendix-ii-setup.step.5` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:appendix-ii-setup.step.6` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:appendix-ii-setup.step.7` | Not enforceable by an engine | — | This record defines printed anatomy, components, or table procedure rather than a game-state decision. |
| `rr:appendix-ii-setup.step.8` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:appendix-ii-setup.step.9` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:appendix-ii-setup.step.10` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:appendix-ii-setup.step.11` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:appendix-ii-setup.step.12` | Covered by a narrower rule | — | A directly cited child clause states the executable decision more precisely. |
| `rr:appendix-ii-setup.step.12.a` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:appendix-ii-setup.step.12.b` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:appendix-ii-setup.step.12.c` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:appendix-ii-setup.step.13` | Outside the supported product boundary | MARVEL-272 | This rule belongs to deckbuilding, campaign, or product classification behavior not yet supported. |
| `rr:appendix-ii-setup.step.14` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:appendix-ii-setup.step.15` | Executable and cited | — | A behavior test directly cites this record. |
| `rr:appendix-ii-setup.step.16` | Executable and cited | — | A behavior test directly cites this record. |
