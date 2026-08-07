from . import *

# Badoon Headhunter

def GetAbilities() -> Sequence['Ability']:

    def badoon_headhunter_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        this.PutIntoPlay(player, effect)


    return [
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            badoon_headhunter_boost
        ),
    ]

