from . import *

# Beauty and the Thief

def GetAbilities() -> Sequence['Ability']:

    def beauty_and_the_thief(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.DealDamage(effect.targets2, 4, effect)
        this.RemoveThreatFromSchemes(effect.targets3, 4, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            beauty_and_the_thief
        ).SetPlay().SetLabel('attack', 'thwart')
        .SetTarget("TeamUp")
        .SetTarget2(Enemy)
        .SetTarget3(Scheme2),
    ]

