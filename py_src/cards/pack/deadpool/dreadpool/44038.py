from . import *

# Dreadpool

def GetAbilities() -> Sequence['Ability']:

    def dreadpool(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetKillerPlayer()
        if player:
            player.DealEncounterCard(this, effect)


    return [
        AbilityFactory.ThisMinionEngageFirstPlayer(),
        AbilityFactory.WhenUnitBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            dreadpool
        ),
    ]

