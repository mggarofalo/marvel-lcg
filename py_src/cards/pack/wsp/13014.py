from . import *

# Surprise Attack

def GetAbilities() -> Sequence['Ability']:

    def surprise_attack(effect: 'Effect', message: 'Message.AfterUnitChangeForm') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        if effect.GetPaidResources().HasColor("R"):
            damage = 4
        else:
            damage = 3
        this.DealDamage(effect.targets, damage, effect)


    return [
        AbilityFactory.AfterUnitChangeForm(
            AbilityType.HeroResponse,
            "You",
            surprise_attack
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy),
    ]

