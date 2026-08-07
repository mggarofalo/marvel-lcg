from . import *

# Panther Claws

def GetAbilities() -> Sequence['Ability']:

    def panther_claws(effect: 'Effect', message: 'Message.WhenResolveSpecialAbility') -> None:
        this = effect.this.CastTo(Upgrade)

        damage = 2
        if effect.this == message.sequence[-1]:
            damage = 4

        this.DealDamage(effect.targets, damage, effect)

    return [
        AbilityFactory.WhenResolveSpecialAbility(
            "This",
            panther_claws
        ).SetLabel('attack')
        .SetTarget(Enemy)
    ]

