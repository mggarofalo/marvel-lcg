from . import *

# Shifting Apparition

def GetAbilities() -> Sequence['Ability']:

    def shifting_apparition_defeated(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetDefeatingPlayer()

        ShuffleTopEncounterDeckIntoPlayerDeck(player, 1, effect)


    return [
        AbilityFactory.WhenUnitBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            shifting_apparition_defeated,
            has_defeating_player=True,
            conditions=[
                lambda effect, message:
                    message.excess_damage > 0
            ]
        ),
    ]

