from . import *

# Upside the Head

def GetAbilities() -> Sequence['Ability']:

    def upside_the_head(effect: 'Effect', message: 'Message.AfterUnitAttackEnd') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        for enemy in effect.targets:
            if Unit2.IsType(enemy):
                if enemy.IsConfused():
                    Faces.GiveStatus([enemy], "Stunned", effect)
                else:
                    Faces.GiveStatus([enemy], "Confused", effect)


    return [
        AbilityFactory.AfterUnitMakeBasicAttack(
            AbilityType.HeroResponse,
            "YourHero",
            upside_the_head,
            damage_who=Enemy,
        ).SetPlay().SetLabel()
        .SetTarget("DamageTarget"),
    ]

