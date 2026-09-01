# Forms

`src/Marvel.Rules/State/Forms.cs`, `src/Marvel.Rules/State/Seat.cs`,
`src/Marvel.Content/CardCatalog.cs`.

The executable scope contains the five Core Set identities. Each has one hero
face and one alter-ego face. The form model is intentionally broader: it was
validated against later printed patterns such as additional keyword forms and
three-sided identities, but those products are not executable. See
[`scope.md`](scope.md).

## Form comes from the board

`rr:identity`: *"A player's identity card is a double-sided card that represents
their hero on one side and their alter-ego on the other. The side that is face
up indicates the form (hero or alter-ego) that player is currently in."*

`Forms.Of` computes form from the faceup cards whenever it is asked. The engine
does not store an `IsHero` flag that could drift from the identity card.

The result is an ordinally sorted set rather than a boolean. That shape supports
`rr:form-change-form.6`, under which a card with a `[type] form` keyword grants
an additional form alongside hero or alter-ego form. Sorting preserves
deterministic iteration. Later card data validates this representation without
making those cards part of the runtime.

`CardCatalog.FormOf` recognizes a keyword only when a complete keyword sentence
has the form `<name> form`. Prose that merely mentions a form is not a keyword.

## Changing identity form

`rr:form-change-form.1` permits one voluntary identity change during a player's
turn each round. The engine separates the operation from the permission:

- `Forms.Change` flips the identity and leaves the rest of the card unchanged.
- the `Change_Form` affordance checks and records `Seat.FormChangedInRound`.

The separation implements `rr:form-change-form.3`: a card ability that changes
form does not spend the voluntary change. A spent voluntary change disappears
from the player's affordances for that round.

Changing form schedules a `FormChanged` occurrence so interrupts and responses
use the normal timing machinery. It does not end the player's turn.

### What survives the flip

`rr:form-change-form.2`: *"When a player changes form, only the form changes."*
Damage, status cards, lasting effects, attachments, tucked cards, tokens, and
ready or exhausted state remain on the same card object. This specific rule
overrides the general cleanup for a flip that changes card type in
`rr:flip.2.2`.

## Additional and three-sided forms

The rules layer can identify an additional keyword-form card and can flip one
through `Forms.ChangeAdditional`. A card ability must still supply the printed
condition that permits the change. No isolated expansion card is admitted to
the executable catalog merely because this general operation exists.

`Forms.Change` refuses an identity with more than two faces. The Rules
Reference defines what counts as flipping a foldable card, but the general rule
does not decide which hero face a change from alter-ego reaches. The engine
raises `RulesNotImplementedException` instead of choosing a face. Supporting a
product with such an identity requires its product rules, setup data, card
abilities, and behavioral scenes to cross the boundary together.

## State contract

The faceup identity face is already represented by the card's face index in
`World.Digest()`. The digest reserves the `f_<name>` namespace for additional
forms. Adding one of those keys is a wire-format change and must be specified
and pinned before an expansion using it becomes executable.

`Seat.FormChangedInRound` is engine state used by the affordance layer. It is
not currently part of the digest, so a digest alone is not a complete save-game
format. See [`state-digest-v2.md`](state-digest-v2.md).
