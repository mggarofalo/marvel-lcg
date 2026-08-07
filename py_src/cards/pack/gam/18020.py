from . import *

# Hit and Run

def GetAbilities() -> Sequence['Ability']:

    def hit_and_run(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.DealDamage(effect.targets, 2, effect)
        this.RemoveThreatFromSchemes(effect.targets2, 2, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            hit_and_run
        ).SetPlay().SetLabel('attack', 'thwart')
        .SetTarget(Enemy)
        .SetTarget2(Scheme2)
    ]

