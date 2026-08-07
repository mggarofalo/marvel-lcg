from . import *

# Ground Stomp

def GetAbilities() -> Sequence['Ability']:

    def ground_stomp(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.DealDamage(effect.targets, 1, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            ground_stomp
        ).SetPlay().SetLabel()
        .SetTarget(Enemy, range="All"),
    ]

