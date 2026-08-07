from . import *

# * Black Tom Cassidy

def GetAbilities() -> Sequence['Ability']:

    def black_tom_cassidy_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()

        minion = Search.EncounterCard(
            effect,
            include_discard_pile=True,
            name="Creeping Willow",
            card_type=Minion
        )

        if minion:
            minion.PutIntoPlay(player, effect)

    return [
        AbilityFactory.UnitCannotTakeDamageWhile(
            AbilityType.NonKeyword,
            "This",
            while_face_is_in_play=CardFinder(name="Creeping Willow")
        ),
        AbilityFactory.WhenThisRevealed(
            None,
            black_tom_cassidy_revealed
        ),
    ]

