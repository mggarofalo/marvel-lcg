from . import *

# Badoon Warlord

def GetAbilities() -> Sequence['Ability']:

    def badoon_warlord_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        message.GainBoostIcon(2, effect)


    return [
        AbilityFactory.UnitAttackGainKeyword(
            "This",
            overkill=True
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            badoon_warlord_boost,
            during_attack=True,
        ),
    ]

