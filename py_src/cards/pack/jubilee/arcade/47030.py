from . import *

# * Arcade

def GetAbilities() -> Sequence['Ability']:

    def arcade_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()

        face = Worlds.DiscardEncounterCardsUntil(
            effect,
            trait="TRAP!",
            card_type=SchemeSide2
        )
        if face:
            face.Reveal(player, effect)


    return [
        AbilityFactory.UnitCannotTakeDamageWhile(
            AbilityType.NonKeyword,
            "This",
            while_face_is_in_play=CardFinder2("TRAP!", SchemeSide2)
        ),
        AbilityFactory.WhenThisRevealed(
            None,
            arcade_revealed
        ),
    ]

