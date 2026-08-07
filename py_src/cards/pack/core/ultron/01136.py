from . import *

# * Ultron (III)

def GetAbilities() -> Sequence['Ability']:

    def ultron_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        face = Search.EncounterCard(
            effect,
            include_discard_pile=True,
            name="Ultron's Imperative",
            card_type=SchemeSide2,
        )
        if face:
            face.Reveal(None, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            ultron_revealed
        ),
        AbilityFactory.UnitCannotTakeDamageWhile(
            AbilityType.NonKeyword,
            "This",
            while_face_is_in_play=CardFinder2("DRONE", Minion),
        ),
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            CardFinder2("DRONE", Minion),
            attack=1,
            health=1,
        ),
    ]
