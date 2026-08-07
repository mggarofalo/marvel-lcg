from . import *

# * Cyclops

def GetAbilities() -> Sequence['Ability']:

    def cyclops(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Hero)
        Unused(this)

        this.DealDamage(effect.targets, 3, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            cyclops
        ).SetLabel('attack')
        .SetName("Optic Blast")
        .SetCost(Cost("1"))
        .SetTarget(Enemy, with_attach=Upgrade)
        .LimitOncePerRound(),
    ]

