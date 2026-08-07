from . import *

# Arachnobatics

def GetAbilities() -> Sequence['Ability']:

    def arachnobatics(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        damage = 2
        if Faces.HasStatus(effect.targets, "Stunned"):
            damage += 3
        if Faces.HasStatus(effect.targets, "Confused"):
            damage += 3
        this.DealDamage(effect.targets, damage, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            arachnobatics
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy),
    ]

