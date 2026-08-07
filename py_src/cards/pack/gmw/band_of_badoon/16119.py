from . import *

# Badoon Lieutenant

def GetAbilities() -> Sequence['Ability']:

    def badoon_lieutenant_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        message.GainBoostIcon(2, effect)


    return [
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            badoon_lieutenant_boost,
            during_scheme=True
        ),
    ]

