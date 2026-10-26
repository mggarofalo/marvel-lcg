# Rapid Response composition prerequisite

Work: MARVEL-78223c8c427-bc9a-4b68-b727-ede712d8e204. Printed authority is
`datasets/cards/cards.json` 08031: after an ally you control is defeated, discard
Rapid Response to put that ally into play from your discard pile and deal it one
damage. This packet uses a test-local interpretation, without ability-book or
product admission.

The imported five-case `BlackWidowRapidResponseCompositionTests` packet was red
at lowering for the proposed engine-chosen selector
`defeatedAllyInYourDiscardPile: "you"`. Its acceptance body had not executed.
Controller at defeat, owner, physical departure and current discard membership
must be proved independently; current ownership cannot establish former control.
