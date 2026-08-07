from . import *

# Time-Displaced Soldier

def GetAbilities() -> Sequence['Ability']:

    def time_displaced_soldier_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        player.DealEncounterCards(1, effect)


    return [
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            time_displaced_soldier_boost
        ),
    ]

